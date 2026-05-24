using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

public class GerenteManager
{
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

        // Chamando a String de Conexão centralizada em Database.cs
        using var conn = new MySqlConnection(Database.GetConnectionString());
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
        catch (MySqlException ex) when (ex.Number == 1062) // Erro de entrada duplicada (UNIQUE)
        {
            Console.WriteLine("\n[ERRO] Já existe uma equipe cadastrada com este nome.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Não foi possível cadastrar: {ex.Message}");
        }
    }

    public static (decimal total, int quantidade, decimal media) ObterMetricasGlobais()
    {
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            // Query ajustada para calcular a média real com base nas porcentagens salvas de cada operação
            string sql = @"
                SELECT 
                    (SELECT IFNULL(SUM(capital_alocado), 0) FROM equipes), 
                    (SELECT COUNT(*) FROM equipes),
                    (SELECT IFNULL(AVG(retorno_porcentagem), 0) FROM operacoes)";
            
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                decimal total = reader.IsDBNull(0) ? 0 : reader.GetDecimal(0);
                int quantidade = reader.GetInt32(1);
                decimal media = reader.IsDBNull(2) ? 0 : reader.GetDecimal(2);
                
                return (total, quantity: quantidade, media);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao carregar métricas: {ex.Message}");
        }
        return (0, 0, 0);
    }

    public static void ListarEquipesBanco()
    {
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            string sql = "SELECT nome_equipe, responsavel FROM equipes ORDER BY nome_equipe ASC";
            
            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine("\n=== EQUIPES EM OPERAÇÃO ===");
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
        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            
            string sql = @"
                SELECT 
                    e.nome_equipe, 
                    e.responsavel, 
                    e.capital_alocado, 
                    e.capital_utilizado,
                    IFNULL(SUM(o.lucro_prejuizo), 0) as pnl_total
                FROM equipes e
                LEFT JOIN operacoes o ON e.id = o.equipe_id
                GROUP BY e.id, e.nome_equipe, e.responsavel, e.capital_alocado, e.capital_utilizado
                ORDER BY pnl_total DESC";

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

    public static void EditarEquipe()
    {
        Console.WriteLine("\n--- EDITAR EQUIPE (PAINEL ADMIN) ---");
        Console.Write("Digite o nome da equipe que deseja editar: ");
        string nomeBusca = Console.ReadLine()!;

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            
            string sqlCheck = "SELECT id FROM equipes WHERE nome_equipe = @nome";
            int equipeId = 0;
            using (var cmdCheck = new MySqlCommand(sqlCheck, conn))
            {
                cmdCheck.Parameters.AddWithValue("@nome", nomeBusca);
                using var reader = cmdCheck.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine("\n[ERRO] Equipe não encontrada.");
                    return;
                }
                equipeId = reader.GetInt32("id");
            }

            Console.WriteLine("\n[Deixe em branco para manter o valor atual]");
            
            Console.Write("Novo Nome da Equipe: ");
            string novoNome = Console.ReadLine()!;
            
            Console.Write("Novo Responsável: ");
            string novoResp = Console.ReadLine()!;

            Console.Write("Novo Capital Alocado (R$): ");
            string capInput = Console.ReadLine()!;
            decimal? novoCapital = null;
            if (!string.IsNullOrWhiteSpace(capInput) && decimal.TryParse(capInput, out decimal cap)) novoCapital = cap;

            string sqlUpdate = @"UPDATE equipes SET 
                                nome_equipe = IF(@nome = '', nome_equipe, @nome),
                                responsavel = IF(@resp = '', responsavel, @resp),
                                capital_alocado = IF(@cap IS NULL, capital_alocado, @cap)
                                WHERE id = @id";

            using var cmdUp = new MySqlCommand(sqlUpdate, conn);
            cmdUp.Parameters.AddWithValue("@nome", novoNome);
            cmdUp.Parameters.AddWithValue("@resp", novoResp);
            cmdUp.Parameters.AddWithValue("@cap", (object?)novoCapital ?? DBNull.Value);
            cmdUp.Parameters.AddWithValue("@id", equipeId);

            cmdUp.ExecuteNonQuery();
            Console.WriteLine("\n[SUCESSO] Dados da equipe atualizados!");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            Console.WriteLine("\n[ERRO] Não foi possível renomear: Já existe outra equipe com este nome.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao editar equipe: {ex.Message}");
        }
    }

    public static void ExcluirEquipe()
    {
        Console.WriteLine("\n--- EXCLUIR EQUIPE (PAINEL ADMIN) ---");
        Console.Write("Digite o nome da equipe que deseja REMOVER permanentemente: ");
        string nome = Console.ReadLine()!;

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"AVISO: Isso apagará TODOS os membros e operações da equipe '{nome}'. Confirma? (S/N): ");
        Console.ResetColor();
        if (Console.ReadLine()!.ToUpper() != "S")
        {
            Console.WriteLine("\nOperação cancelada.");
            return;
        }

        using var conn = new MySqlConnection(Database.GetConnectionString());
        try
        {
            conn.Open();
            string sql = "DELETE FROM equipes WHERE nome_equipe = @nome";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@nome", nome);

            int linhasAfetadas = cmd.ExecuteNonQuery();

            if (linhasAfetadas > 0)
                Console.WriteLine("\n[SUCESSO] Equipe e todos os seus vínculos foram removidos do sistema.");
            else
                Console.WriteLine("\n[AVISO] Nenhuma equipe encontrada com este nome.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERRO] Falha ao excluir equipe: {ex.Message}");
        }
    }
}