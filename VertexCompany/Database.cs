using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

public class Database
{    
    public static string GetConnectionString()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
        return config.GetConnectionString("DefaultConnection")!;
    }

    public static void Inicializar()
    {
        using var connection = new MySqlConnection(GetConnectionString());
        try
        {
            connection.Open();

            // 1. Tabela de Equipes (Garantindo charset UTF-8 para nomes e senhas)
            string sqlEquipes = @"
                CREATE TABLE IF NOT EXISTS equipes (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    nome_equipe VARCHAR(100) NOT NULL UNIQUE,
                    responsavel VARCHAR(100) NOT NULL,
                    senha_equipe VARCHAR(255) NOT NULL,
                    senha_admin VARCHAR(255) NOT NULL,
                    capital_alocado DECIMAL(15, 2) DEFAULT 0.00,
                    capital_utilizado DECIMAL(15, 2) DEFAULT 0.00,
                    retorno_total DECIMAL(15, 2) DEFAULT 0.00
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            // 2. Tabela de Membros (Suporte seguro para o enum 'Férias')
            string sqlMembros = @"
                CREATE TABLE IF NOT EXISTS membros (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    equipe_id INT,
                    nome VARCHAR(100) NOT NULL,
                    status ENUM('Ativo', 'Férias') DEFAULT 'Ativo',
                    FOREIGN KEY (equipe_id) REFERENCES equipes(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            // 3. Tabela de Operações (Histórico Financeiro alinhado com o EquipeManager)
            string sqlOperacoes = @"
                CREATE TABLE IF NOT EXISTS operacoes (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    equipe_id INT,
                    ativo VARCHAR(100) NOT NULL,
                    tipo VARCHAR(50),           
                    valor_aplicado DECIMAL(15, 2) NOT NULL,
                    lucro_prejuizo DECIMAL(15, 2) DEFAULT 0.00,
                    retorno_porcentagem DECIMAL(5, 2) DEFAULT 0.00,
                    data_operacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (equipe_id) REFERENCES equipes(id) ON DELETE CASCADE
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            ExecuteCommand(sqlEquipes, connection);
            ExecuteCommand(sqlMembros, connection);
            ExecuteCommand(sqlOperacoes, connection);
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"\n[ERRO FATAL] Não foi possível inicializar o banco de dados: {ex.Message}");
        }
    }

    private static void ExecuteCommand(string sql, MySqlConnection conn)
    {
        using var cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }
}