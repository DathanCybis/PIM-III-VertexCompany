using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

public class EquipeManager
{
    private static string GetConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        return config.GetConnectionString("DefaultConnection")!;
    }

    public static void RegistrarOperacao(string nomeEquipe)
    {
        Console.WriteLine($"\n--- REGISTRAR OPERAÇÃO: EQUIPE {nomeEquipe.ToUpper()} ---");

        Console.Write("Ativo (Ex: Tesouro Selic, Ações Petrobras): ");
        string ativo = Console.ReadLine()!;

        Console.Write("Tipo (Ex: Conservador, Arriscado): ");
        string tipo = Console.ReadLine()!;

        Console.Write("Valor Aplicado (R$): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal valor)) valor = 0;

        Console.Write("Lucro/Prejuízo Estimado (R$) [Use sinal de '-' para prejuízo]: ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal lucro)) lucro = 0;

        // Cálculo automático da porcentagem de retorno
        decimal porcentagem = valor > 0 ? (lucro / valor) * 100 : 0;

        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            
            // Primeiro, pegamos o ID da equipe pelo nome
            string sqlId = "SELECT id FROM equipes WHERE nome_equipe = @nome";
            int equipeId = 0;
            using (var cmdId = new MySqlCommand(sqlId, conn))
            {
                cmdId.Parameters.AddWithValue("@nome", nomeEquipe);
                equipeId = Convert.ToInt32(cmdId.ExecuteScalar());
            }

            // Inserimos a operação
            string sqlOp = @"INSERT INTO operacoes (equipe_id, ativo, tipo, valor_aplicado, lucro_prejuizo, retorno_porcentagem) 
                             VALUES (@id, @ativo, @tipo, @valor, @lucro, @porc)";

            using var cmdOp = new MySqlCommand(sqlOp, conn);
            cmdOp.Parameters.AddWithValue("@id", equipeId);
            cmdOp.Parameters.AddWithValue("@ativo", ativo);
            cmdOp.Parameters.AddWithValue("@tipo", tipo);
            cmdOp.Parameters.AddWithValue("@valor", valor);
            cmdOp.Parameters.AddWithValue("@lucro", lucro);
            cmdOp.Parameters.AddWithValue("@porc", porcentagem);

            cmdOp.ExecuteNonQuery();

            // Atualizamos o capital utilizado na tabela de equipes (Soma de todas as operações)
            string sqlUpdate = @"UPDATE equipes SET capital_utilizado = (SELECT SUM(valor_aplicado) FROM operacoes WHERE equipe_id = @id) 
                                 WHERE id = @id";
            using var cmdUp = new MySqlCommand(sqlUpdate, conn);
            cmdUp.Parameters.AddWithValue("@id", equipeId);
            cmdUp.ExecuteNonQuery();

            Console.WriteLine("\n[SUCESSO] Operação registrada e capital atualizado!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao registrar operação: {ex.Message}");
        }
    }


    public static void ExibirResumoFinanceiro(string nomeEquipe)
    {
        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            
            // Consulta para pegar os dados da equipe e o P&L das operações
            string sql = @"
                SELECT e.capital_alocado, e.capital_utilizado, 
                    SUM(o.lucro_prejuizo) as pnl_total,
                    COUNT(o.id) as total_operacoes
                FROM equipes e
                LEFT JOIN operacoes o ON e.id = o.equipe_id
                WHERE e.nome_equipe = @nome
                GROUP BY e.id";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nomeEquipe);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                decimal alocado = reader.GetDecimal(0);
                decimal utilizado = reader.GetDecimal(1);
                decimal pnl = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                int totalOps = reader.GetInt32(3);

                decimal saldoDisponivel = alocado - utilizado;
                decimal porcentagemUso = alocado > 0 ? (utilizado / alocado) * 100 : 0;

                Console.WriteLine($"\n=== RESUMO FINANCEIRO: {nomeEquipe.ToUpper()} ===");
                Console.WriteLine("---------------------------------------------------");
                Console.WriteLine($"Capital Alocado:     {alocado:C}");
                Console.WriteLine($"Total Aplicado:      {utilizado:C}");
                Console.WriteLine($"Saldo Disponível:    {saldoDisponivel:C}");
                Console.WriteLine($"Uso do Capital:      {porcentagemUso:F2}%");
                Console.WriteLine("---------------------------------------------------");
                
                // Lógica de cor para o P&L (Lucros e Perdas)
                if (pnl > 0) Console.ForegroundColor = ConsoleColor.Green;
                else if (pnl < 0) Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"P&L Líquido:         {pnl:C} ({(pnl >= 0 ? "Positivo" : "Negativo")})");
                Console.ResetColor();
                
                Console.WriteLine($"Operações Ativas:    {totalOps}");
                Console.WriteLine("---------------------------------------------------");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao carregar resumo: {ex.Message}");
        }
    }

    public static void AdicionarMembro(string nomeEquipe)
    {
        Console.WriteLine($"\n--- CADASTRAR MEMBRO NA EQUIPE {nomeEquipe.ToUpper()} ---");
        Console.Write("Nome do Membro: ");
        string nome = Console.ReadLine()!;
        
        Console.WriteLine("Status: [1] Ativo | [2] De Férias");
        string statusOp = Console.ReadLine()!;
        string status = (statusOp == "2") ? "Férias" : "Ativo";

        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            
            // Pegamos o ID da equipe
            string sqlId = "SELECT id FROM equipes WHERE nome_equipe = @nome";
            int equipeId = 0;
            using (var cmdId = new MySqlCommand(sqlId, conn))
            {
                cmdId.Parameters.AddWithValue("@nome", nomeEquipe);
                equipeId = Convert.ToInt32(cmdId.ExecuteScalar());
            }

            // Inserimos o membro
            string sqlMembro = "INSERT INTO membros (equipe_id, nome, status) VALUES (@id, @nome, @status)";
            using var cmd = new MySqlCommand(sqlMembro, conn);
            cmd.Parameters.AddWithValue("@id", equipeId);
            cmd.Parameters.AddWithValue("@nome", nome);
            cmd.Parameters.AddWithValue("@status", status);

            cmd.ExecuteNonQuery();
            Console.WriteLine("\n[SUCESSO] Membro registrado com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao adicionar membro: {ex.Message}");
        }
    }

    public static void ListarMembros(string nomeEquipe)
    {
        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            string sql = @"
                SELECT m.nome, m.status 
                FROM membros m
                JOIN equipes e ON m.equipe_id = e.id
                WHERE e.nome_equipe = @nome";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nomeEquipe);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine($"\n=== MEMBROS DA EQUIPE: {nomeEquipe.ToUpper()} ===");
            Console.WriteLine("---------------------------------------------------");

            if (!reader.HasRows)
            {
                Console.WriteLine("Nenhum membro vinculado a esta equipe.");
            }

            while (reader.Read())
            {
                string nome = reader.GetString(0);
                string status = reader.GetString(1);
                
                // UX: Muda a cor dependendo do status
                if (status == "Férias") Console.ForegroundColor = ConsoleColor.Yellow;
                
                Console.WriteLine($"- {nome.PadRight(20)} | Status: {status}");
                Console.ResetColor();
            }
            Console.WriteLine("---------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao listar membros: {ex.Message}");
        }
    }








}
