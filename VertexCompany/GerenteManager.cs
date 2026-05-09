using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;

public class GerenteManager
{
    private static string GetConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        return config.GetConnectionString("DefaultConnection")!;
    }

    public static void CadastrarEquipe()
    {
        Console.WriteLine("\n--- CADASTRO DE NOVA EQUIPE ---");
        
        Console.Write("Nome da Equipe: ");
        string nome = Console.ReadLine()!;
        
        Console.Write("Responsável: ");
        string resp = Console.ReadLine()!;
        
        Console.Write("Senha da Equipe: ");
        string senhaEq = Console.ReadLine()!;
        
        Console.Write("Senha Administrativa (para esta equipe): ");
        string senhaAd = Console.ReadLine()!;
        
        Console.Write("Capital Alocado Inicial (Ex: 500000): ");
        if (!decimal.TryParse(Console.ReadLine(), out decimal capital)) capital = 0;

        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            string sql = @"INSERT INTO equipes (nome_equipe, responsavel, senha_equipe, senha_admin, capital_alocado) 
                           VALUES (@nome, @resp, @sEq, @sAd, @cap)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nome);
            cmd.Parameters.AddWithValue("@resp", resp);
            cmd.Parameters.AddWithValue("@sEq", senhaEq);
            cmd.Parameters.AddWithValue("@sAd", senhaAd);
            cmd.Parameters.AddWithValue("@cap", capital);

            cmd.ExecuteNonQuery();
            Console.WriteLine("\n[SUCESSO] Equipe cadastrada com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Não foi possível cadastrar: {ex.Message}");
        }
    }

    public static (decimal total, int quantidade) ObterMetricasGlobais()
    {
        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            // SQL que soma o capital e conta as equipes em uma única consulta
            string sql = "SELECT SUM(capital_alocado), COUNT(*) FROM equipes";
            
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                // Tratamos o null caso o banco esteja vazio
                decimal total = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                int quantidade = reader.GetInt32(1);
                return (total, quantidade);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar visão consolidada: {ex.Message}");
        }
        return (0, 0);
    }


    public static void ListarEquipesBanco()
    {
        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            string sql = "SELECT nome_equipe, responsavel FROM equipes ORDER BY nome_equipe ASC";
            
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine("\n=== EQUIPES EM OPERAÇÃO (VIA DATABASE) ===");
            Console.WriteLine("---------------------------------------------------");

            if (!reader.HasRows)
            {
                Console.WriteLine("Nenhuma equipe cadastrada no momento.");
            }

            while (reader.Read())
            {
                string nome = reader.GetString(0);
                string resp = reader.GetString(1);
                Console.WriteLine($"- Equipe {nome} | Responsável: {resp}");
            }
            
            Console.WriteLine("---------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao listar equipes: {ex.Message}");
        }
    }

    public static void ExibirRelatorioGeral()
    {
        using var conn = new MySqlConnection(GetConnectionString());
        try
        {
            conn.Open();
            
            // SQL robusto: Soma lucros, calcula ROI e traz dados da equipe
            string sql = @"
                SELECT 
                    e.nome_equipe, 
                    e.responsavel, 
                    e.capital_alocado, 
                    e.capital_utilizado,
                    IFNULL(SUM(o.lucro_prejuizo), 0) as pnl_total
                FROM equipes e
                LEFT JOIN operacoes o ON e.id = o.equipe_id
                GROUP BY e.id
                ORDER BY pnl_total DESC"; // Ranking: Quem lucra mais aparece primeiro

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine("\n===========================================================");
            Console.WriteLine("                RELATÓRIO ESTRATÉGICO DE P&L               ");
            Console.WriteLine("===========================================================");
            Console.WriteLine(string.Format("{0,-12} | {1,-12} | {2,-10} | {3,-10}", "EQUIPE", "CAP. ALOC", "UTILIZADO", "LUCRO/PREJ"));
            Console.WriteLine("-----------------------------------------------------------");

            decimal lucroGlobal = 0;

            while (reader.Read())
            {
                string nome = reader.GetString(0);
                decimal alocado = reader.GetDecimal(2);
                decimal utilizado = reader.GetDecimal(3);
                decimal pnl = reader.GetDecimal(4);
                lucroGlobal += pnl;

                // Lógica de cores para o P&L individual
                if (pnl > 0) Console.ForegroundColor = ConsoleColor.Green;
                else if (pnl < 0) Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(string.Format("{0,-12} | {1,-12:C0} | {2,-10:C0} | {3,-10:C2}", 
                    nome, alocado, utilizado, pnl));
                
                Console.ResetColor();
            }

            Console.WriteLine("-----------------------------------------------------------");
            Console.Write("LUCRO LÍQUIDO TOTAL: ");
            
            if (lucroGlobal > 0) Console.ForegroundColor = ConsoleColor.Green;
            else if (lucroGlobal < 0) Console.ForegroundColor = ConsoleColor.Red;
            
            Console.WriteLine($"{lucroGlobal:C}");
            Console.ResetColor();
            Console.WriteLine("===========================================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao gerar relatório: {ex.Message}");
        }
    }




}
