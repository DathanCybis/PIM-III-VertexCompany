using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
public class Database
{    public static string GetConnectionString()
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
        connection.Open();

        // 1. Tabela de Equipes (Foco em Capital e Responsável)
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
            );";

        // 2. Tabela de Membros (Com Status Ativo/Férias)
        string sqlMembros = @"
            CREATE TABLE IF NOT EXISTS membros (
                id INT AUTO_INCREMENT PRIMARY KEY,
                equipe_id INT,
                nome VARCHAR(100) NOT NULL,
                status ENUM('Ativo', 'Férias') DEFAULT 'Ativo',
                FOREIGN KEY (equipe_id) REFERENCES equipes(id) ON DELETE CASCADE
            );";

        // 3. Tabela de Operações (Histórico Financeiro)
        string sqlOperacoes = @"
            CREATE TABLE IF NOT EXISTS operacoes (
                id INT AUTO_INCREMENT PRIMARY KEY,
                equipe_id INT,
                ativo VARCHAR(100) NOT NULL, -- Ex: Tesouro Direto
                tipo VARCHAR(50),           -- Ex: Conservador
                valor_aplicado DECIMAL(15, 2) NOT NULL,
                lucro_prejuizo DECIMAL(15, 2) DEFAULT 0.00,
                retorno_porcentagem DECIMAL(5, 2) DEFAULT 0.00,
                data_operacao TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (equipe_id) REFERENCES equipes(id) ON DELETE CASCADE
            );";

        ExecuteCommand(sqlEquipes, connection);
        ExecuteCommand(sqlMembros, connection);
        ExecuteCommand(sqlOperacoes, connection);
    }

    private static void ExecuteCommand(string sql, MySqlConnection conn)
    {
        using var cmd = new MySqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }











}

