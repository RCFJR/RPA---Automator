# RPA Configurável por CSV

Um aplicativo desktop Windows (.NET + WPF) que automatiza tarefas repetitivas usando sequências de cliques, esperas e preenchimento de campos com dados de um arquivo CSV.

## 🎯 Objetivo

Eliminar o trabalho manual de cadastro repetitivo (cliente por cliente, linha por linha) automatizando:
- Cliques em coordenadas de tela
- Esperas entre ações
- Digitação de texto com placeholders vinculados a colunas do CSV (ex: `{{Nome}}`, `{{CPF}}`)
- Pressionamento de teclas (Enter, Tab, atalhos)
- Ativação de janelas por título

## 📂 Estrutura do Projeto

```
RPA/
├─ src/
│  ├─ RPA.Core              → Modelos de domínio, placeholders, CSV loader
│  ├─ RPA.Automation        → Simulação de input (SharpHook), ativação de janelas (Win32)
│  ├─ RPA.Execution         → Motor de execução (loop CSV × Steps)
│  ├─ RPA.Persistence       → Persistência JSON de fluxos
│  └─ RPA.App               → Interface WPF (telas de gravação, edição, execução)
├─ tests/
│  ├─ RPA.Core.Tests        → Testes de resolução de placeholders e CSV
│  └─ RPA.Execution.Tests   → Testes do motor de execução
└─ RPA.sln
```

## 🔧 Stack Técnico

- **.NET 8.0** (net8.0-windows7.0 para módulos que usam Win32/WPF)
- **WPF** (interface desktop)
- **SharpHook 5.3.9** (hooks globais para gravação + simulação de entrada)
- **CsvHelper 33.1.0** (leitura de CSV)
- **CommunityToolkit.Mvvm 8.4.2** (MVVM pattern)
- **xUnit + Moq** (testes unitários)

## 🚀 Como Compilar

```bash
cd C:\Projects\RPA
dotnet build
```

Para rodar testes:
```bash
dotnet test
```

## 📋 Modelos Core

### Flow (Fluxo)
```csharp
public record Flow
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string? TargetWindowTitle { get; init; }
    public List<Step> Steps { get; init; }
}
```

### Step (Passo)
Tipos suportados:
- **ActivateWindowStep**: Ativa janela por título
- **WaitStep**: Espera (em ms)
- **ClickStep**: Clique em coordenada (x,y) com origem Screen ou WindowRelative
- **TypeTextStep**: Digita texto com resolução de placeholders (modo DirectInput ou Clipboard)
- **KeyPressStep**: Pressiona sequência de teclas

### CSV
- Carregado via `CsvLoader` (CsvHelper)
- Cada linha = um `CsvRow` (dicionário `string → string`)
- Colunas usadas como placeholders (ex: `{{Nome}}` busca valor da coluna "Nome")

## 🎮 Fluxo de Uso

1. **Carregar CSV**: Selecionar arquivo, visualizar colunas
2. **Criar/Abrir Fluxo**: Nova sequência de steps ou editar existente
3. **Gravar** (futuro): Ativar gravador, clicar no sistema alvo, o RPA captura coordenadas/digitação
4. **Editar**: Ajustar steps, trocar textos fixos por placeholders (ex: `João` → `{{Nome}}`)
5. **Testar**: Executar um step isolado ou todo fluxo em modo de visualização
6. **Executar**: Rodar fluxo para cada linha do CSV, gerar log de resultado

## 🔌 Componentes Implementados

### ✅ Completos
- **RPA.Core**: Modelos (Flow, Step, CsvRow), resolução de placeholders, leitor de CSV
- **RPA.Automation**: Simulador de input (SharpHook), ativador de janelas (Win32)
- **RPA.Execution**: Motor de execução, executor de steps, logging de resultado
- **RPA.Persistence**: Serialização JSON de Flow
- **RPA.App**: Interface WPF (layout básico)
- **Testes Unitários**: PlaceholderResolver, FlowExecutionEngine

### 🚧 Em Desenvolvimento
- **RecorderService**: Gravador de ações (hook global com SharpHook — em progresso)
- **ViewModel/MVVM**: Bindings WPF para telas de edição e execução
- **UI/Interação**: Telas funcionais (adicionar, remover, editar steps; carregar CSV; executar)

## 🧪 Testes

Testes já criados e passando:
- Resolução de placeholders (incluindo acentuação PT-BR)
- Extração de placeholders de templates
- Execução de fluxo com múltiplas linhas
- Tratamento de erros (continuar/abortar por linha)

## 📝 Próximos Passos (V1)

1. **Completar gravador** (RecorderService com SharpHook eventos corretos)
2. **MVVM ViewModels**:
   - `FlowEditorViewModel`: Gerenciar steps (add, remove, reorder)
   - `ExecutionViewModel`: Rodar fluxo, atualizar progresso, pausar/retomar
   - `CsvLoaderViewModel`: Carregar e visualizar CSV
3. **Telas WPF funcionais**:
   - Editor de fluxo (lista de steps + painel de propriedades)
   - Gravador (botão start/stop + listagem de ações)
   - Execução (ProgressBar, botões pause/resume/stop, log em tempo real)
4. **Persistência de fluxos** (salvar/carregar JSON)
5. **Testes E2E** (fluxo completo: gravar → editar → executar)

## ⚙️ Configuração de Desenvolvimento

- **IDE**: Visual Studio 2022, VS Code (com extensão C#) ou JetBrains Rider
- **.NET SDK**: 8.0+
- **NuGet Packages**: Ver .csproj files

## 📞 Notas

- Coordenadas são salvas por padrão em `CoordinateOrigin.WindowRelative` para portabilidade entre sessões/monitores
- Simulação de digitação usa `SharpHook.EventSimulator.SimulateTextEntry()` para suportar acentuação PT-BR sem depender de layout
- Motor de execução suporta pausa/retomada via `SemaphoreSlim`
- Cancelamento global via `CancellationToken` + hotkey global (futuro: `Ctrl+Shift+Q`)

---

**Estado**: Arquitetura core implementada ✅ | UI em progresso 🚧 | Gravador ajustes finais 🔧
