# Exemplos de Uso — RPA Configurável

## Exemplo 1: Modelo de Flow (Fluxo)

```csharp
var flow = new Flow
{
    Name = "Cadastro de Clientes",
    TargetWindowTitle = "Sistema Web de Cadastro",
    Steps = new List<Step>
    {
        new ActivateWindowStep
        {
            WindowTitleContains = "Sistema Web",
            Label = "Ativar janela do navegador"
        },
        new ClickStep
        {
            X = 100,
            Y = 50,
            Origin = CoordinateOrigin.WindowRelative,
            Label = "Clicar campo Nome",
            Kind = ClickKind.Single,
            Button = MouseButtonKind.Left
        },
        new TypeTextStep
        {
            Template = "{{Nome}}",
            Mode = TypeMode.DirectInput,
            Label = "Digitar nome do cliente"
        },
        new KeyPressStep
        {
            Keys = new List<string> { "Tab" },
            Label = "Passar para próximo campo"
        },
        new TypeTextStep
        {
            Template = "{{CPF}}",
            Mode = TypeMode.DirectInput,
            Label = "Digitar CPF"
        },
        new ClickStep
        {
            X = 200,
            Y = 300,
            Origin = CoordinateOrigin.WindowRelative,
            Label = "Clicar botão Salvar"
        },
        new WaitStep
        {
            Milliseconds = 2000,
            Label = "Aguardar salvar"
        }
    }
};
```

## Exemplo 2: Arquivo CSV de Entrada

`clientes.csv`:
```csv
Nome,CPF,Email,Telefone
João Silva,123.456.789-00,joao@example.com,(11) 98765-4321
Maria Santos,987.654.321-00,maria@example.com,(21) 99876-5432
Pedro Oliveira,456.789.123-00,pedro@example.com,(31) 97654-3210
```

## Exemplo 3: Executando o Fluxo

```csharp
// Carregar CSV
var csvLoader = new CsvLoader();
var (headers, rows) = await csvLoader.LoadAsync("clientes.csv");

// Criar engine com dependências
var inputSimulator = new InputSimulatorService();
var windowActivator = new WindowActivatorService();
var placeholderResolver = new PlaceholderResolver();
var stepExecutor = new StepExecutor(inputSimulator, windowActivator, placeholderResolver);
var engine = new FlowExecutionEngine(stepExecutor);

// Configurar opções
var options = new ExecutionOptions
{
    OnErrorPolicy = OnErrorPolicy.Continue,  // Se falhar uma linha, continua próxima
    StepDelayMs = 100                        // Pequeno delay entre steps para estabilidade
};

// Executar com progress
var progress = new Progress<ExecutionProgress>(p =>
{
    Console.WriteLine($"Processando linha {p.CurrentRowIndex + 1}/{p.TotalRows}: {p.CurrentStep}");
});

var results = await engine.RunAsync(flow, rows, options, progress, CancellationToken.None);

// Verificar resultados
foreach (var result in results)
{
    if (result.Success)
        Console.WriteLine($"✓ Linha {result.RowIndex + 1}: Sucesso ({result.Duration.TotalSeconds:F1}s)");
    else
        Console.WriteLine($"✗ Linha {result.RowIndex + 1}: ERRO — {result.ErrorMessage}");
}

// Exportar log
var exporter = new ExecutionLogExporter();
await exporter.ExportToCsvAsync(results, "resultado_execucao.csv");
```

## Exemplo 4: Resolver Placeholders

```csharp
var resolver = new PlaceholderResolver();

var template = "Nome: {{Nome}}, CPF: {{CPF}}, Email: {{Email}}";
var rowValues = new Dictionary<string, string>
{
    { "Nome", "João Silva" },
    { "CPF", "123.456.789-00" },
    { "Email", "joao@example.com" }
};

var resolved = resolver.Resolve(template, rowValues);
// Resultado: "Nome: João Silva, CPF: 123.456.789-00, Email: joao@example.com"

// Extrair placeholder usados
var placeholders = resolver.ExtractPlaceholders(template);
// Resultado: ["Nome", "CPF", "Email"]
```

## Exemplo 5: Salvar/Carregar Flow

```csharp
var repository = new FlowRepository();

// Salvar
var flow = new Flow { Name = "Meu Fluxo", Steps = new List<Step> { /* ... */ } };
await repository.SaveAsync(flow, "fluxo_cadastro.rpaflow.json");

// Carregar
var loadedFlow = await repository.LoadAsync("fluxo_cadastro.rpaflow.json");
```

## Exemplo 6: Pause/Resume do Motor

```csharp
var engine = new FlowExecutionEngine(stepExecutor);

var executionTask = engine.RunAsync(flow, rows, options, progress, ct);

// Em outra thread, pausar após 5 segundos
_ = Task.Delay(5000).ContinueWith(_ => engine.Pause());

// Retomar após 10 segundos
_ = Task.Delay(10000).ContinueWith(_ => engine.Resume());

var results = await executionTask;
```

## Exemplo 7: Criar Flow Manualmente (sem Gravador)

```csharp
var flow = new Flow
{
    Id = Guid.NewGuid(),
    Name = "Teste no Notepad",
    TargetWindowTitle = "Notepad",
    Steps = new List<Step>
    {
        new ActivateWindowStep { WindowTitleContains = "Notepad" },
        new ClickStep { X = 100, Y = 100, Origin = CoordinateOrigin.WindowRelative },
        new TypeTextStep { Template = "Olá {{Nome}}, seja bem-vindo!", Mode = TypeMode.DirectInput },
        new KeyPressStep { Keys = new List<string> { "Enter" } },
        new WaitStep { Milliseconds = 500 }
    }
};

var csvLoader = new CsvLoader();
var (headers, rows) = await csvLoader.LoadAsync("nomes.csv");
// nomes.csv:
// Nome
// João
// Maria

var results = await engine.RunAsync(flow, rows, options, null, CancellationToken.None);
// Resultado: Notepad vai ter digitado:
// "Olá João, seja bem-vindo!"
// "Olá Maria, seja bem-vindo!"
```

## Exemplo 8: Lidar com Erros

```csharp
var options = new ExecutionOptions
{
    OnErrorPolicy = OnErrorPolicy.Continue  // Tenta todas as linhas
};

var results = await engine.RunAsync(flow, rows, options, null, CancellationToken.None);

// Analisar o que falhou
var falhadas = results.Where(r => !r.Success).ToList();
if (falhadas.Any())
{
    foreach (var falha in falhadas)
    {
        Console.WriteLine($"Linha {falha.RowIndex + 1}: {falha.ErrorMessage}");
    }

    // Opção: reprocessar só as que falharam
    var rowsParaReprocessar = rows.Where(r => falhadas.Any(f => f.RowIndex == r.RowIndex)).ToList();
    // ... executar novamente ...
}
```

## Exemplo 9: Tipo de Erro Esperado

```csharp
// Se a janela alvo não for encontrada:
// → ActivateWindowStep joga InvalidOperationException
// → Com OnErrorPolicy.Continue, log fica com ErrorMessage = "Window containing 'título' not found."
// → Com OnErrorPolicy.Abort, a execução inteira para

// Se um placeholder não resolver (coluna não existe no CSV):
// → TypeTextStep digita o placeholder literalmente: "{{CPFInvalido}}"
// → RECOMENDAÇÃO: validar placeholders antes de rodar (implementar em ViewModel)
```

## Exemplo 10: Teste Unitário

```csharp
[Fact]
public async Task RunAsync_ResolvesCsvPlaceholders_InTypeTextSteps()
{
    var mockStepExecutor = new Mock<IStepExecutor>();
    var engine = new FlowExecutionEngine(mockStepExecutor.Object);

    var flow = new Flow
    {
        Steps = new List<Step>
        {
            new TypeTextStep { Template = "{{Nome}}" }
        }
    };

    var row = new CsvRow { RowIndex = 0 };
    row["Nome"] = "João";
    var rows = new List<CsvRow> { row };

    await engine.RunAsync(flow, rows, new ExecutionOptions(), null, CancellationToken.None);

    // Verificar que o StepExecutor foi chamado com os valores resolvidos
    mockStepExecutor.Verify(x => x.ExecuteAsync(
        It.IsAny<TypeTextStep>(),
        It.Is<IReadOnlyDictionary<string, string>>(d => d["Nome"] == "João"),
        It.IsAny<CancellationToken>()
    ));
}
```

---

**Nota**: Esses exemplos são pseudocódigo/parciais. A implementação completa requer MVVM ViewModels e UI WPF bindings que ainda estão em desenvolvimento.
