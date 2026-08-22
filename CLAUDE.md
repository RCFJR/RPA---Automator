# CLAUDE.md — RPA Configurável por CSV

## Projeto

**Aplicação RPA desktop (.NET 8 + WPF)** que automatiza cadastros repetitivos carregando um CSV e executando uma sequência configurada de cliques, esperas e digitação com placeholders para cada linha do arquivo.

Exemplo de fluxo: abrir sistema web de cadastro → clicar campo Nome → digitar `{{Nome}}` (substitui com valor da coluna "Nome" do CSV) → Tab → digitar `{{CPF}}` → clicar Salvar → aguardar 2s → próxima linha.

## Tech Stack

- **.NET 8** com **WPF** (interface desktop)
- **SharpHook 5.3.9** (simulação de input + hooks globais para gravador)
- **CsvHelper** (leitura de CSV)
- **CommunityToolkit.Mvvm** (MVVM pattern)
- **xUnit + Moq** (testes)

### Decisões Técnicas Fixas

1. **Coordenadas de tela** (não UI Automation) — escolha deliberada do usuário; funciona tanto web quanto desktop sem driver específico
2. **Origem WindowRelative** por padrão — cliques salvos relativos à janela alvo, não à tela, para portabilidade entre sessões
3. **SharpHook único** para simulação + gravador (descarta InputSimulator antigos de 2018)
4. **Serialização JSON** de Flow com `JsonDerivedType` para polimorfismo de Step
5. **SemaphoreSlim** para pausa/retomada do motor, `CancellationToken` para cancelamento global

## Estrutura

```
src/
  RPA.Core         — modelos (Flow, Step, CsvRow), PlaceholderResolver, CsvLoader
  RPA.Automation   — SharpHook (input + futuro gravador), Win32 (janelas)
  RPA.Execution    — motor (loop CSV × Steps), StepExecutor, logging
  RPA.Persistence  — JSON FlowRepository
  RPA.App          — WPF MainWindow + MVVM (ViewModels ainda não implementados)
tests/
  RPA.Core.Tests   — placeholder resolution, CSV loading
  RPA.Execution.Tests — motor execution, error handling
```

## Estado

✅ **Completo**:
- Modelagem de domínio (Flow, Step, CsvRow)
- PlaceholderResolver (regex {{ColName}})
- CsvLoader (CsvHelper)
- InputSimulatorService (SharpHook)
- WindowActivatorService (Win32)
- FlowExecutionEngine (motor com pausa/retomada)
- FlowRepository (JSON)
- Testes unitários (placeholder, motor)

🚧 **Em progresso**:
- RecorderService (SharpHook eventos — estrutura criada, tipos de evento pendentes)
- ViewModels MVVM
- UI WPF funcional (layout criado, DataBindings não implementados)

## Próximos Passos

1. **Completar gravador** — conectar SharpHook MouseClicked/KeyTyped para capturar ações reais
2. **MVVM**: FlowEditorViewModel, ExecutionViewModel, CsvLoaderViewModel
3. **UI**: Wiring de DataBindings (adicionar/remover steps, carregar CSV, rodar execução)
4. **Teste E2E**: Fluxo completo com Notepad como alvo

## Como Compilar e Testar

```bash
dotnet build              # Deve compilar sem erros (1 aviso SDK é ignorado)
dotnet test               # Testes unitários
dotnet run -p src/RPA.App # Roda a aplicação WPF
```

## Onde Começar (para próxima sessão)

1. **RecorderService**: Ajustar tipos de evento do SharpHook (`Global.Common.EventArgs`?)
2. **MainWindow binding**: Criar command binding para botões "Novo", "Gravar", "Executar"
3. **ExecutionViewModel**: Implementar engine.RunAsync em background, report progress
4. **Teste ponta-a-ponta**: Bloco de Notas como alvo

## Arquivos Críticos

| Arquivo | Papel |
|---------|-------|
| `src/RPA.Core/Models/Step.cs` | Tipos de Step (ActivateWindow, Click, TypeText, etc.) |
| `src/RPA.Core/Services/PlaceholderResolver.cs` | Resolve `{{ColName}}` em templates |
| `src/RPA.Execution/Services/FlowExecutionEngine.cs` | Loop principal: para cada linha CSV, executa Steps |
| `src/RPA.Automation/Services/InputSimulatorService.cs` | Clique/digitação/teclas via SharpHook |
| `src/RPA.Automation/Services/WindowActivatorService.cs` | Ativa janela por título |
| `src/RPA.App/MainWindow.xaml` | Interface WPF (5 abas) |
| `tests/RPA.Core.Tests/UnitTest1.cs` | Testes de placeholder |
| `tests/RPA.Execution.Tests/UnitTest1.cs` | Testes de motor |

## Notas de Desenvolvimento

- **Coordenadas**: Salvas como `(X, Y, CoordinateOrigin.WindowRelative)`. Na execução: recalculadas baseado no retângulo atual da janela alvo.
- **Digitação com acentos PT-BR**: Use `InputSimulatorService.TypeTextAsync()` que chama `SharpHook.EventSimulator.SimulateTextEntry()`— isso cuida de Unicode corretamente.
- **Pausa**: Implementada com `SemaphoreSlim` checado a cada Step. Não há fila — é just um `WaitAsync()` bloqueador.
- **Cancelamento**: `CancellationToken` checado antes de cada Step. Futuro: hotkey global `Ctrl+Shift+Q` ligada ao token.

## Estilo de Código

- Sem comentários desnecessários (código bem-nomeado é suficiente)
- Records para imutabilidade (Flow, Step, CsvRow)
- Injeção de dependência via construtores
- Async/await para I/O
- Testes com Moq para isolamento de Win32
