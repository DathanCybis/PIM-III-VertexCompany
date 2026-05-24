using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;

public class EquipeManager
{
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

        using var conn = new MySqlConnection(Database.GetConnectionString());
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
            string sqlUpdate = @"UPDATE equipes SET capital_utilizado = (SELECT IFNULL(SUM(valor_aplicado), 0) FROM operacoes WHERE equipe_id = @id) 
                                 WHERE id = @id";
            using var cmdUp = new MySqlCommand(sqlUpdate, conn);
            cmdUp.Parameters.AddWithValue("@id", equipeId);
            cmdUp.ExecuteNonQuery();

            Console.WriteLine("\n[SUCESSO] Operação registrada e capital updated!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao registrar operação: {ex.Message}");
        }
    }

    public static void ExibirResumoFinanceiro(string nomeEquipe)
    {
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            
            string sql = @"
                SELECT e.capital_alocado, IFNULL(e.capital_utilizado, 0), 
                    IFNULL(SUM(o.lucro_prejuizo), 0) as pnl_total,
                    COUNT(o.id) as total_operacoes
                FROM equipes e
                LEFT JOIN operacoes o ON e.id = o.equipe_id
                WHERE e.nome_equipe = @nome
                GROUP BY e.id, e.capital_alocado, e.capital_utilizado";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nomeEquipe);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                decimal alocado = reader.GetDecimal(0);
                decimal utilizado = reader.GetDecimal(1);
                decimal pnl = reader.GetDecimal(2);
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

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            
            string sqlId = "SELECT id FROM equipes WHERE nome_equipe = @nome";
            int equipeId = 0;
            using (var cmdId = new MySqlCommand(sqlId, conn))
            {
                cmdId.Parameters.AddWithValue("@nome", nomeEquipe);
                equipeId = Convert.ToInt32(cmdId.ExecuteScalar());
            }

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
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            string sql = @"
                SELECT m.id, m.nome, m.status 
                FROM membros m
                JOIN equipes e ON m.equipe_id = e.id
                WHERE e.nome_equipe = @nome";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nomeEquipe);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine($"\n=== MEMBROS DA EQUIPE: {nomeEquipe.ToUpper()} ===");
            Console.WriteLine("---------------------------------------------------");
            Console.WriteLine("ID   | Nome                 | Status");
            Console.WriteLine("---------------------------------------------------");

            if (!reader.HasRows)
            {
                Console.WriteLine("Nenhum membro vinculado a esta equipe.");
            }

            while (reader.Read())
            {
                int id = reader.GetInt32(0);       
                string nome = reader.GetString(1);     
                string status = reader.GetString(2);   
                
                if (status == "Férias") Console.ForegroundColor = ConsoleColor.Yellow;
                
                Console.WriteLine($"{id,-4} | {nome.PadRight(20)} | Status: {status}");
                Console.ResetColor();
            }
            Console.WriteLine("---------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao listar membros: {ex.Message}");
        }
    }

    public static void ListarOperacoesDaEquipe(string nomeEquipe)
    {
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            string sql = @"SELECT o.id, o.ativo, o.tipo, o.valor_aplicado, o.lucro_prejuizo, o.retorno_porcentagem 
                        FROM operacoes o 
                        JOIN equipes e ON o.equipe_id = e.id 
                        WHERE e.nome_equipe = @nome";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nomeEquipe);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine($"\n=== OPERAÇÕES REGISTRADAS: {nomeEquipe.ToUpper()} ===");
            Console.WriteLine("ID  | Ativo                | Tipo        | Valor (R$)   | P&L (R$)    | Retorno");
            Console.WriteLine("-----------------------------------------------------------------------------");
            
            while (reader.Read())
            {
                Console.WriteLine($"{reader.GetInt32("id"),-3} | " +
                                $"{reader.GetString("ativo").PadRight(20)} | " +
                                $"{reader.GetString("tipo").PadRight(11)} | " +
                                $"{reader.GetDecimal("valor_aplicado"),-12:C} | " +
                                $"{reader.GetDecimal("lucro_prejuizo"),-11:C} | " +
                                $"{reader.GetDecimal("retorno_porcentagem")}%");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao listar operações: {ex.Message}");
        }
    }

    public static void EditarOperacao(string nomeEquipe)
    {
        ListarOperacoesDaEquipe(nomeEquipe);

        Console.Write("\nDigite o ID da operação que deseja editar: ");
        if (!int.TryParse(Console.ReadLine(), out int opId)) return;

        Console.WriteLine("\n[Deixe em branco para manter o valor atual]");
        Console.Write("Novo Ativo: ");
        string novoAtivo = Console.ReadLine()!;
        
        Console.Write("Novo Tipo: ");
        string novoTipo = Console.ReadLine()!;

        Console.Write("Novo Valor Aplicado (R$): ");
        string vInput = Console.ReadLine()!;
        decimal? novoValor = null;
        if (!string.IsNullOrWhiteSpace(vInput) && decimal.TryParse(vInput, out decimal v)) novoValor = v;

        Console.Write("Novo Lucro/Prejuízo Estimado (R$): ");
        string lInput = Console.ReadLine()!;
        decimal? novoLucro = null;
        if (!string.IsNullOrWhiteSpace(lInput) && decimal.TryParse(lInput, out decimal l)) novoLucro = l;

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();

            string sqlGetOp = "SELECT valor_aplicado, lucro_prejuizo, equipe_id FROM operacoes WHERE id = @id";
            decimal valAtual = 0, lucAtual = 0;
            int equipeId = 0;
            using (var cmdGet = new MySqlCommand(sqlGetOp, conn))
            {
                cmdGet.Parameters.AddWithValue("@id", opId);
                using var r = cmdGet.ExecuteReader();
                if (r.Read())
                {
                    valAtual = r.GetDecimal("valor_aplicado");
                    lucAtual = r.GetDecimal("lucro_prejuizo");
                    equipeId = r.GetInt32("equipe_id");
                }
                else
                {
                    Console.WriteLine("\n[ERRO] Operação não encontrada.");
                    return;
                }
            }

            decimal valFinal = novoValor ?? valAtual;
            decimal lucFinal = novoLucro ?? lucAtual;
            decimal novaPorcentagem = valFinal > 0 ? (lucFinal / valFinal) * 100 : 0;

            string sqlUpdate = @"UPDATE operacoes SET 
                                ativo = IF(@ativo = '', ativo, @ativo),
                                tipo = IF(@tipo = '', tipo, @tipo),
                                valor_aplicado = @valor,
                                lucro_prejuizo = @lucro,
                                retorno_porcentagem = @porc
                                WHERE id = @id";

            using var cmdUp = new MySqlCommand(sqlUpdate, conn);
            cmdUp.Parameters.AddWithValue("@ativo", novoAtivo);
            cmdUp.Parameters.AddWithValue("@tipo", novoTipo);
            cmdUp.Parameters.AddWithValue("@valor", valFinal);
            cmdUp.Parameters.AddWithValue("@lucro", lucFinal);
            cmdUp.Parameters.AddWithValue("@porc", novaPorcentagem);
            cmdUp.Parameters.AddWithValue("@id", opId);
            cmdUp.ExecuteNonQuery();

            string sqlRecalculo = @"UPDATE equipes SET capital_utilizado = (SELECT IFNULL(SUM(valor_aplicado), 0) FROM operacoes WHERE equipe_id = @eqId) 
                                    WHERE id = @eqId";
            using var cmdRecalc = new MySqlCommand(sqlRecalculo, conn);
            cmdRecalc.Parameters.AddWithValue("@eqId", equipeId);
            cmdRecalc.ExecuteNonQuery();

            Console.WriteLine("\n[SUCESSO] Operação atualizada e capital da equipe recalculado!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao editar operação: {ex.Message}");
        }
    }

    public static void ExcluirOperacao(string nomeEquipe)
    {
        ListarOperacoesDaEquipe(nomeEquipe);

        Console.Write("\nDigite o ID da operação que deseja EXCLUIR: ");
        if (!int.TryParse(Console.ReadLine(), out int opId)) return;

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();

            string sqlGetId = "SELECT equipe_id FROM operacoes WHERE id = @id";
            int equipeId = 0;
            using (var cmdGet = new MySqlCommand(sqlGetId, conn))
            {
                cmdGet.Parameters.AddWithValue("@id", opId);
                var res = cmdGet.ExecuteScalar();
                if (res != null) equipeId = Convert.ToInt32(res);
            }

            if (equipeId == 0)
            {
                Console.WriteLine("\n[ERRO] Operação não encontrada.");
                return;
            }

            string sqlDel = "DELETE FROM operacoes WHERE id = @id";
            using var cmdDel = new MySqlCommand(sqlDel, conn);
            cmdDel.Parameters.AddWithValue("@id", opId);
            cmdDel.ExecuteNonQuery();

            string sqlRecalculo = @"UPDATE equipes SET capital_utilizado = (SELECT IFNULL(SUM(valor_aplicado), 0) FROM operacoes WHERE equipe_id = @eqId) 
                                    WHERE id = @eqId";
            using var cmdRecalc = new MySqlCommand(sqlRecalculo, conn);
            cmdRecalc.Parameters.AddWithValue("@eqId", equipeId);
            cmdRecalc.ExecuteNonQuery();

            Console.WriteLine("\n[SUCESSO] Operação removida e saldo do capital atualizado!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao excluir operação: {ex.Message}");
        }
    }

    public static void EditarMembro(string nomeEquipe)
    {
        Console.Clear();
        Console.WriteLine($"--- EDITAR MEMBRO - EQUIPE {nomeEquipe.ToUpper()} ---");
        
        ListarMembros(nomeEquipe);

        Console.Write("\nDigite o ID do membro que deseja editar (ou 0 para cancelar): ");
        if (!int.TryParse(Console.ReadLine(), out int idMembro) || idMembro == 0) return;

        Console.Write("Digite o novo Nome do membro: ");
        string novoNome = Console.ReadLine()!;
        
        Console.WriteLine("Novo Status: [1] Ativo | [2] De Férias");
        string statusOp = Console.ReadLine()!;
        string novoStatus = (statusOp == "2") ? "Férias" : "Ativo";

        if (string.IsNullOrWhiteSpace(novoNome))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[ERRO] O nome do membro não pode ficar em branco.");
            Console.ResetColor();
            return;
        }

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();

            // 1. Buscamos o ID numérico da equipe pelo nome recebido no parâmetro do método
            string sqlId = "SELECT id FROM equipes WHERE nome_equipe = @nomeEquipe";
            int equipeId = 0;
            using (var cmdId = new MySqlCommand(sqlId, conn))
            {
                cmdId.Parameters.AddWithValue("@nomeEquipe", nomeEquipe);
                equipeId = Convert.ToInt32(cmdId.ExecuteScalar());
            }

            // 2. Query corrigida usando os parâmetros mapeados corretamente
            string sql = @"UPDATE membros SET nome = @nome, status = @status 
                           WHERE id = @id AND equipe_id = @equipeId";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", novoNome);
            cmd.Parameters.AddWithValue("@status", novoStatus);
            cmd.Parameters.AddWithValue("@id", idMembro);
            cmd.Parameters.AddWithValue("@equipeId", equipeId); // Vinculado corretamente ao ID numérico

            int linhasAfetadas = cmd.ExecuteNonQuery();

            if (linhasAfetadas > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCESSO] Dados do membro atualizados com sucesso!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERRO] Membro não encontrado ou não pertence à sua equipe.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao atualizar membro: {ex.Message}");
        }
        Console.ResetColor();
    }

    public static void ExcluirMembro(string nomeEquipe)
    {
        Console.Clear();
        Console.WriteLine($"--- REMOVER MEMBRO - EQUIPE {nomeEquipe.ToUpper()} ---");
        
        ListarMembros(nomeEquipe);

        Console.Write("\nDigite o ID do membro que deseja REMOVER (ou 0 para cancelar): ");
        if (!int.TryParse(Console.ReadLine(), out int idMembro) || idMembro == 0) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"\nTem certeza que deseja remover o membro ID {idMembro}? (S/N): ");
        Console.ResetColor();
        string confirmacao = Console.ReadLine()!.ToUpper();

        if (confirmacao != "S")
        {
            Console.WriteLine("\nOperação cancelada.");
            return;
        }

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();

            // 1. Buscamos o ID numérico da equipe primeiro
            string sqlId = "SELECT id FROM equipes WHERE nome_equipe = @nomeEquipe";
            int equipeId = 0;
            using (var cmdId = new MySqlCommand(sqlId, conn))
            {
                cmdId.Parameters.AddWithValue("@nomeEquipe", nomeEquipe);
                equipeId = Convert.ToInt32(cmdId.ExecuteScalar());
            }

            // 2. Query executada filtrando por ID numérico da equipe
            string sql = "DELETE FROM membros WHERE id = @id AND equipe_id = @equipeId";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", idMembro);
            cmd.Parameters.AddWithValue("@equipeId", equipeId); // Parâmetro agora bate com a query SQL

            int linhasAfetadas = cmd.ExecuteNonQuery();

            if (linhasAfetadas > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n[SUCESSO] Membro removido do sistema.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n[ERRO] Membro não encontrado ou não pertence à sua equipe.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao remover membro: {ex.Message}");
        }
        Console.ResetColor();
    }
}
