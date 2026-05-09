using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;
Database.Inicializar();

// Simulação de dados (Posteriormente virão do Banco de Dados)
decimal retornoMedio = 12.5m;

bool executando = true;

while (executando)
{

    var metricas = GerenteManager.ObterMetricasGlobais();
    decimal capitalTotalAlocado = metricas.total;
    int equipesAtivas = metricas.quantidade;

    Console.WriteLine("\n===========================================================");
    Console.WriteLine("                VERTEX COMPANY - SISTEMA FINANCEIRO         ");
    Console.WriteLine("                                                           ");
    Console.WriteLine("            - Gerencie equipes e capital com precisão -      ");
    Console.WriteLine("===========================================================");

    Console.WriteLine("\n[1] Sobre o Projeto");
    Console.WriteLine("[2] Nossa Visão");
    Console.WriteLine("[3] Equipes Ativas");
    Console.WriteLine("[4] Acesso Restrito (Gerente/Equipe)");
    Console.WriteLine("[0] Sair");

    Console.WriteLine("\n-----------------------------------------------------------");
    Console.WriteLine("VISÃO CONSOLIDADA (REAL-TIME)");
    Console.WriteLine($"Capital Total Alocado: {capitalTotalAlocado:C}");
    Console.WriteLine($"Retorno Médio das Equipes: {retornoMedio}%");
    Console.WriteLine($"Equipes em Operação: {equipesAtivas}");
    Console.WriteLine("-----------------------------------------------------------");

    Console.Write("\nSelecione uma opção: ");
    string opcao = Console.ReadLine()!;

    switch (opcao)
    {
        case "1": ExibirSobre(); break;
        case "2": ExibirVisao(); break;
        case "3": ListarEquipesAtivas(); break;
        case "4": MenuLogin(); break;
        case "0": executando = false; break;
        default: 
            Console.WriteLine("\nOpção inválida. Pressione qualquer tecla."); 
            Console.ReadKey(); 
            break;
    }
}

// --- MÉTODOS DE CONTEÚDO ---

void ExibirSobre()
{
    Console.WriteLine("\n=== SOBRE O PROJETO VERTEX COMPANY ===");
    Console.WriteLine("A Vertex Company é uma plataforma de gestão para empresas do mercado financeiro");
    Console.WriteLine("que operam com múltiplas equipes e capital próprio alocado por operação.");
    Console.WriteLine("\nPrincipais recursos:");
    Console.WriteLine(" - Alocação de verba por equipe");
    Console.WriteLine(" - Monitoramento de performance");
    Console.WriteLine(" - Controle de gastos operacionais");
    Console.WriteLine(" - Relatórios de P&L por período");
    Console.WriteLine(" - Gestão de membros e funções");
    Console.WriteLine("\nPressione qualquer tecla para voltar...");
    Console.ReadKey();
}

void ExibirVisao()
{
    Console.WriteLine("\n=== NOSSA VISÃO ===");
    Console.WriteLine("Decisões financeiras de alto impacto exigem ferramentas à altura. Nossa missão");
    Console.WriteLine("é oferecer clareza operacional para que gestores foquem no que importa: resultados.");
    Console.WriteLine("\n\"Controle total sobre cada centavo alocado — em tempo real, para cada equipe.\"");
    Console.WriteLine("\nPressione qualquer tecla para voltar...");
    Console.ReadKey();
}

void ListarEquipesAtivas()
{
    GerenteManager.ListarEquipesBanco();
    
    Console.WriteLine("\nPressione qualquer tecla para voltar...");
    Console.ReadKey();
}

void MenuLogin()
{
    Console.WriteLine("\n=== ACESSO RESTRITO - VERTEX COMPANY ===");
    Console.WriteLine("1. Entrar como Gerente");
    Console.WriteLine("2. Entrar como Equipe");
    Console.WriteLine("0. Voltar");
    Console.Write("\nEscolha o nível de acesso: ");
    
    string nivel = Console.ReadLine()!;
    if (nivel == "0") return;

    Console.Write("Identificação (Nome da Equipe): ");
    string usuario = Console.ReadLine()!;
    Console.Write("Senha de Acesso: ");
    string senha = Console.ReadLine()!;

    using var conn = new MySqlConnection(Database.GetConnectionString());
    try
    {
        conn.Open();
        string sql = "";

        // Se for Gerente (Nível 1), verifica a senha_admin
        if (nivel == "1")
        {
            sql = "SELECT nome_equipe FROM equipes WHERE nome_equipe = @nome AND senha_admin = @senha";
        }
        // Se for Equipe (Nível 2), verifica a senha_equipe
        else if (nivel == "2")
        {
            sql = "SELECT nome_equipe FROM equipes WHERE nome_equipe = @nome AND senha_equipe = @senha";
        }

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nome", usuario);
        cmd.Parameters.AddWithValue("@senha", senha);

        var resultado = cmd.ExecuteScalar();

        if (resultado != null)
        {
            string nomeConfirmado = resultado.ToString()!;
            
            if (nivel == "1") 
            {
                Console.WriteLine($"\n[OK] Acesso Administrativo concedido para {nomeConfirmado}.");
                PainelGerente(); 
            }
            else 
            {
                Console.WriteLine($"\n[OK] Acesso Operacional concedido para Equipe {nomeConfirmado}.");
                PainelEquipe(nomeConfirmado);
            }
        }
        else
        {
            Console.WriteLine("\n[ERRO] Credenciais inválidas para o nível selecionado.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERRO] Falha na conexão: {ex.Message}");
    }

    Console.WriteLine("\nPressione qualquer tecla para continuar...");
    Console.ReadKey();
}

void PainelGerente()
{
    bool noPainel = true;
    while (noPainel)
    {
        Console.WriteLine("\n--- PAINEL ESTRATÉGICO (GERENTE) ---");
        Console.WriteLine("1. Cadastrar Nova Equipe");
        Console.WriteLine("2. Listar Métricas Globais (P&L Consolidado)");
        Console.WriteLine("0. Sair do Painel");
        Console.Write("\nOpção: ");
        
        string op = Console.ReadLine()!;

        if (op == "1")
        {
            GerenteManager.CadastrarEquipe();
        }
        else if (op == "2")
        {
            GerenteManager.ExibirRelatorioGeral();
            Console.WriteLine("\nPressione qualquer tecla para voltar...");
            Console.ReadKey();
        }
        else if (op == "0")
        {
            noPainel = false;
        }
    }
}

void PainelEquipe(string nomeEquipe)
{
    bool noPainel = true;
    while (noPainel)
    {
        Console.WriteLine($"\n--- PAINEL OPERACIONAL (EQUIPE {nomeEquipe.ToUpper()}) ---");
        Console.WriteLine("1. Registrar Nova Operação");
        Console.WriteLine("2. Ver Resumo Financeiro");
        Console.WriteLine("3. Listar Membros da Equipe");
        Console.WriteLine("4. Adicionar Novo Membro");
        Console.WriteLine("0. Sair");
        Console.Write("\nOpção: ");

        string op = Console.ReadLine()!;

        if (op == "1") EquipeManager.RegistrarOperacao(nomeEquipe);
        else if (op == "2") {
            EquipeManager.ExibirResumoFinanceiro(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para voltar...");
            Console.ReadKey();
        }
        else if (op == "3") {
            EquipeManager.ListarMembros(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para voltar...");
            Console.ReadKey();
        }
        else if (op == "4") EquipeManager.AdicionarMembro(nomeEquipe);
        else if (op == "0") noPainel = false;
    }
}
