using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System;

// Inicializa o banco de dados criando as tabelas com suporte a acentuação se não existirem
Database.Inicializar();

bool executando = true;

while (executando)
{
    // Recupera métricas atualizadas em tempo real do banco de dados
    var (capitalTotalAlocado, equipesAtivas, retornoMedio) = GerenteManager.ObterMetricasGlobais();

    Console.Clear(); // Limpa o console a cada ciclo para manter o layout elegante
    Console.WriteLine("===========================================================");
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

    // Aplicando cor dinâmica para a média de retorno global
    if (retornoMedio > 0) Console.ForegroundColor = ConsoleColor.Green;
    else if (retornoMedio < 0) Console.ForegroundColor = ConsoleColor.Red;

    Console.WriteLine($"Retorno Médio Global:  {retornoMedio:F2}%");
    Console.ResetColor();
    
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
            Console.WriteLine("\n[AVISO] Opção inválida. Pressione qualquer tecla para tentar novamente."); 
            Console.ReadKey(); 
            break;
    }
}

// --- MÉTODOS DE CONTEÚDO ---

void ExibirSobre()
{
    Console.Clear();
    Console.WriteLine("=== SOBRE O PROJETO VERTEX COMPANY ===");
    Console.WriteLine("\nA Vertex Company é uma plataforma de gestão para empresas do mercado financeiro");
    Console.WriteLine("que operam com múltiplas equipes e capital próprio alocado por operação.");
    Console.WriteLine("\nPrincipais recursos:");
    Console.WriteLine(" - Alocação de verba paramétrica por equipe");
    Console.WriteLine(" - Monitoramento de performance e P&L líquido");
    Console.WriteLine(" - Controle de status operacional de membros");
    Console.WriteLine(" - Relatórios consolidados automáticos em tempo real");
    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu principal...");
    Console.ReadKey();
}

void ExibirVisao()
{
    Console.Clear();
    Console.WriteLine("=== NOSSA VISÃO ESTRATÉGICA ===");
    Console.WriteLine("\nDecisões financeiras de alto impacto exigem ferramentas estáveis. Nossa missão");
    Console.WriteLine("é oferecer clareza operacional para que gestores foquem no que importa: resultados.");
    Console.WriteLine("\n\"Controle analítico sobre cada centavo alocado — em tempo real, para cada equipe.\"");
    Console.WriteLine("\nPressione qualquer tecla para voltar ao menu principal...");
    Console.ReadKey();
}

void ListarEquipesAtivas()
{
    Console.Clear();
    GerenteManager.ListarEquipesBanco();
    
    Console.WriteLine("\nPressione qualquer tecla para voltar...");
    Console.ReadKey();
}

void MenuLogin()
{
    Console.Clear();
    Console.WriteLine("=== ACESSO RESTRITO - VERTEX COMPANY ===");
    Console.WriteLine("1. Entrar como Gerente (Painel administrativo)");
    Console.WriteLine("2. Entrar como Equipe (Painel equipes)");
    Console.WriteLine("0. Voltar");
    Console.Write("\nEscolha o nível de acesso: ");
    
    string nivel = Console.ReadLine()!;
    if (nivel == "0" || (nivel != "1" && nivel != "2")) return;

    Console.Write("\nIdentificação (Nome da Equipe): ");
    string usuario = Console.ReadLine()!;
    Console.Write("Senha de Acesso: ");
    string senha = Console.ReadLine()!;

    using var conn = new MySqlConnection(Database.GetConnectionString());
    try
    {
        conn.Open();
        string sql = "";

        // Se for acesso de Gerente Administrador
        if (nivel == "1")
        {
            // Permite login master via usuário genérico admin OU conferindo as chaves cadastrais da equipe
            sql = @"SELECT nome_equipe FROM equipes 
                    WHERE (nome_equipe = @nome AND senha_admin = @senha) 
                    OR (@nome = 'admin' AND senha_admin = @senha) LIMIT 1";
        }
        // Se for acesso operacional da equipe técnica
        else if (nivel == "2")
        {
            sql = "SELECT nome_equipe FROM equipes WHERE nome_equipe = @nome AND senha_equipe = @senha";
        }

        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@nome", usuario);
        cmd.Parameters.AddWithValue("@senha", senha);

        var resultado = cmd.ExecuteScalar();

        if (resultado != null || (nivel == "1" && senha == "admin")) // Fallback de contingência para primeiro login
        {
            string nomeConfirmado = resultado?.ToString() ?? "Administrador Geral";
            
            if (nivel == "1") 
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SUCESSO] Acesso Administrativo concedido. Bem-vindo, {nomeConfirmado}.");
                Console.WriteLine("Pressione qualquer tecla para abrir o Painel Estratégico...");
                Console.ReadKey();
                PainelGerente(); 
            }
            else 
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SUCESSO] Acesso Operacional liberado para a Equipe: {nomeConfirmado}.");
                Console.ResetColor();
                Console.WriteLine("Pressione qualquer tecla para abrir o Painel Operacional...");
                Console.ReadKey();
                PainelEquipe(nomeConfirmado);
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n[ERRO] Credenciais inválidas ou nível de acesso incorreto.");
            Console.ResetColor();
            Console.WriteLine("\nPressione qualquer tecla para continuar...");
            Console.ReadKey();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERRO] Falha crítica de autenticação: {ex.Message}");
        Console.WriteLine("\nPressione qualquer tecla para continuar...");
        Console.ReadKey();
    }
}

void PainelGerente()
{
    bool noPainel = true;
    while (noPainel)
    {
        Console.Clear();
        Console.WriteLine("--- PAINEL ESTRATÉGICO (GERENTE MASTER) ---");
        Console.WriteLine("1. Cadastrar Nova Equipe");
        Console.WriteLine("2. Listar Métricas Globais (P&L Consolidado)");
        Console.WriteLine("3. Editar Informações de Equipe");
        Console.WriteLine("4. Excluir Equipe do Sistema");
        Console.WriteLine("0. Sair do Painel");
        Console.Write("\nOpção: ");
        
        string op = Console.ReadLine()!;

        if (op == "1")
        {
            GerenteManager.CadastrarEquipe();
        }
        else if (op == "2")
        {
            Console.Clear();
            GerenteManager.ExibirRelatorioGeral();
            Console.WriteLine("\nPressione qualquer tecla para retornar ao painel...");
            Console.ReadKey();
        }
        else if (op == "3")
        {
            GerenteManager.EditarEquipe();
            Console.WriteLine("\nPressione qualquer tecla para retornar ao painel...");
            Console.ReadKey();
        }
        else if (op == "4")
        {
            GerenteManager.ExcluirEquipe();
            Console.WriteLine("\nPressione qualquer tecla para retornar ao painel...");
            Console.ReadKey();
        }
        else if (op == "0")
        {
            noPainel = false;
        }
        else
        {
            Console.WriteLine("\n[AVISO] Opção inválida dentro do painel administrativo.");
            Console.ReadKey();
        }
    }
}

void PainelEquipe(string nomeEquipe)
{
    bool noPainel = true;
    while (noPainel)
    {
        Console.Clear();
        Console.WriteLine($"--- PAINEL OPERACIONAL (EQUIPE: {nomeEquipe.ToUpper()}) ---");
        
        // Bloco de Operações Financeiras
        Console.WriteLine("1. Registrar Nova Operação");
        Console.WriteLine("2. Editar Dados de Operação");
        Console.WriteLine("3. Excluir Operação");
        Console.WriteLine("4. Listar Operações Realizadas"); // <-- Nova opção dedicada aqui!
        
        // Bloco de Gestão de Membros
        Console.WriteLine("5. Listar Membros da Equipe");
        Console.WriteLine("6. Adicionar Novo Membro Técnico");
        Console.WriteLine("7. Editar Dados de Membro"); 
        Console.WriteLine("8. Remover Membro"); 
        
        // Indicadores e Saída
        Console.WriteLine("9. Ver Resumo Financeiro da Equipe");
        Console.WriteLine("0. Fazer Logout");
        Console.Write("\nOpção: ");

        string op = Console.ReadLine()!;

        // 1. Registrar Nova Operação
        if (op == "1") EquipeManager.RegistrarOperacao(nomeEquipe);
        
        // 2. Editar Dados de Operação
        else if (op == "2") {
            Console.Clear();
            EquipeManager.EditarOperacao(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 3. Excluir Operação
        else if (op == "3") {
            Console.Clear();
            EquipeManager.ExcluirOperacao(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }

        // 4. Listar Operações Realizadas
        else if (op == "4") {
            Console.Clear();
            EquipeManager.ListarOperacoesDaEquipe(nomeEquipe); // <-- Chamada do método do EquipeManager
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 5. Listar Membros da Equipe
        else if (op == "5") {
            Console.Clear();
            EquipeManager.ListarMembros(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 6. Adicionar Novo Membro Técnico
        else if (op == "6") EquipeManager.AdicionarMembro(nomeEquipe);
        
        // 7. Editar Dados de Membro
        else if (op == "7") {
            EquipeManager.EditarMembro(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 8. Remover Membro
        else if (op == "8") {
            EquipeManager.ExcluirMembro(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 9. Ver Resumo Financeiro da Equipe
        else if (op == "9") {
            Console.Clear();
            EquipeManager.ExibirResumoFinanceiro(nomeEquipe);
            Console.WriteLine("\nPressione qualquer tecla para retornar...");
            Console.ReadKey();
        }
        
        // 0. Fazer Logout
        else if (op == "0") noPainel = false;
        
        // Tratamento de entradas inválidas
        else
        {
            Console.WriteLine("\n[AVISO] Opção inválida dentro do painel operacional.");
            Console.ReadKey();
        }
    }
}
