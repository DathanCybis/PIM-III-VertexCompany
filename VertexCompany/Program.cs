using System;

// Simulação de dados (Posteriormente virão do Banco de Dados)
decimal capitalTotalAlocado = 2500000.00m;
decimal retornoMedio = 12.5m;
int equipesAtivas = 4;

bool executando = true;

while (executando)
{
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
    Console.WriteLine("\n=== EQUIPES EM OPERAÇÃO ===");
    Console.WriteLine("---------------------------------------------------");
    Console.WriteLine("- Equipe Alpha  | Responsável: Ana Lima");
    Console.WriteLine("- Equipe Beta   | Responsável: Carlos Melo");
    Console.WriteLine("- Equipe Delta  | Responsável: Fernanda Cruz");
    Console.WriteLine("- Equipe Omega  | Responsável: Pedro Nunes");
    Console.WriteLine("---------------------------------------------------");
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

    Console.Write("Identificação (Nome/ID): ");
    string usuario = Console.ReadLine()!;
    Console.Write("Senha: ");
    string senha = Console.ReadLine()!;

    if (nivel == "1")
    {
        // Aqui chamaremos a validação do Gerente no DB futuramente
        if (usuario == "admin" && senha == "vertex2026") 
        {
            PainelGerente();
        }
        else 
        {
            Console.WriteLine("\n[ERRO] Credenciais de Gerência inválidas.");
        }
    }
    else if (nivel == "2")
    {
        // Aqui chamaremos a validação da Equipe no DB futuramente
        // Simulando login da Equipe Alpha
        if (usuario == "Alpha" && senha == "123") 
        {
            PainelEquipe("Alpha");
        }
        else 
        {
            Console.WriteLine("\n[ERRO] Nome de equipe ou senha incorretos.");
        }
    }
    
    Console.WriteLine("\nPressione qualquer tecla para continuar...");
    Console.ReadKey();
}

void PainelGerente()
{
    Console.WriteLine("\n--- PAINEL ESTRATÉGICO (GERENTE) ---");
    Console.WriteLine("> Métricas Consolidadas");
    Console.WriteLine("> Gestão de Capital Alocado");
    Console.WriteLine("> Performance por Equipe (P&L)");
    Console.WriteLine("> Adicionar/Remover Equipes");
    Console.WriteLine("\n[Em desenvolvimento: Integração com SQL]");
}

void PainelEquipe(string nomeEquipe)
{
    Console.WriteLine($"\n--- PAINEL OPERACIONAL (EQUIPE {nomeEquipe.ToUpper()}) ---");
    Console.WriteLine("> Resumo Financeiro da Equipe");
    Console.WriteLine("> Operações Ativas (Tesouro, Ações, etc)");
    Console.WriteLine("> Listar Membros da Equipe");
    Console.WriteLine("> Registrar Nova Operação");
    Console.WriteLine("\n[Em desenvolvimento: Filtro por Equipe_ID]");
}
