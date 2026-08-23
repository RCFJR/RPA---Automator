using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RPA.Automation;
using RPA.Automation.Recording;
using RPA.Core.Models;
using RPA.Core.Services;
using RPA.Persistence.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace RPA.App.ViewModels;

public partial class FlowEditorViewModel : ObservableObject
{
    private readonly IFlowRepository _flowRepository;
    private readonly IPlaceholderResolver _placeholderResolver;
    private readonly IPositionCaptureService _positionCaptureService;

    [ObservableProperty]
    private Flow currentFlow = new();

    [ObservableProperty]
    private ObservableCollection<Step> steps = new();

    [ObservableProperty]
    private int selectedIndex = -1;

    [ObservableProperty]
    private Step? selectedStep;

    [ObservableProperty]
    private string flowName = "Novo Fluxo";

    [ObservableProperty]
    private string targetWindowTitle = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private string[] stepTypeOptions = new[]
    {
        "ActivateWindow",
        "Wait",
        "Click",
        "TypeText",
        "KeyPress"
    };

    [ObservableProperty]
    private bool usesCsv = true;

    [ObservableProperty]
    private int repeatCount = 1;

    private List<string> _availableColumns = new();

    public FlowEditorViewModel(IFlowRepository flowRepository, IPlaceholderResolver placeholderResolver, IPositionCaptureService positionCaptureService)
    {
        _flowRepository = flowRepository ?? throw new ArgumentNullException(nameof(flowRepository));
        _placeholderResolver = placeholderResolver ?? throw new ArgumentNullException(nameof(placeholderResolver));
        _positionCaptureService = positionCaptureService ?? throw new ArgumentNullException(nameof(positionCaptureService));
        CreateNewFlow();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        SelectedStep = (value >= 0 && value < Steps.Count) ? Steps[value] : null;
    }

    partial void OnUsesCsvChanged(bool value)
    {
        UpdateExecutionModeDisplay();
    }

    partial void OnRepeatCountChanged(int value)
    {
        UpdateExecutionModeDisplay();
    }

    public bool UsesRepeat => !UsesCsv;

    public string ExecutionModeHint => UsesCsv
        ? "Carregue um CSV e o fluxo será executado uma vez por linha"
        : $"O fluxo será repetido {RepeatCount}x sem ler dados de um arquivo";

    public Visibility RepeatCountVisibility => UsesCsv ? Visibility.Collapsed : Visibility.Visible;

    private void UpdateExecutionModeDisplay()
    {
        OnPropertyChanged(nameof(UsesRepeat));
        OnPropertyChanged(nameof(ExecutionModeHint));
        OnPropertyChanged(nameof(RepeatCountVisibility));
    }

    [RelayCommand]
    public void CreateNewFlow()
    {
        CurrentFlow = new Flow
        {
            Name = "Novo Fluxo",
            Steps = new List<Step>(),
            ExecutionSource = new RepeatExecutionSource(1)
        };
        FlowName = CurrentFlow.Name;
        TargetWindowTitle = CurrentFlow.TargetWindowTitle ?? string.Empty;
        Steps.Clear();
        SelectedIndex = -1;
        UsesCsv = true;
        RepeatCount = 1;
        StatusMessage = "✨ Novo fluxo criado";
    }

    [RelayCommand]
    public async Task SaveFlow()
    {
        if (string.IsNullOrWhiteSpace(FlowName))
        {
            StatusMessage = "Nome do fluxo é obrigatório";
            return;
        }

        try
        {
            var executionSource = UsesCsv
                ? CurrentFlow.ExecutionSource ?? new CsvExecutionSource(new List<CsvRow>())
                : new RepeatExecutionSource(RepeatCount);

            CurrentFlow = CurrentFlow with
            {
                Name = FlowName,
                TargetWindowTitle = TargetWindowTitle,
                Steps = Steps.ToList(),
                ExecutionSource = executionSource
            };

            var fileName = $"{FlowName.Replace(" ", "_")}.rpaflow.json";
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RPA", fileName);

            await _flowRepository.SaveAsync(CurrentFlow, filePath);
            StatusMessage = $"✓ Fluxo '{FlowName}' salvo com sucesso!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao salvar: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadFlow()
    {
        var openDialog = new OpenFileDialog
        {
            Filter = "Fluxos RPA (*.rpaflow.json)|*.rpaflow.json|Todos os arquivos (*.*)|*.*",
            InitialDirectory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RPA")
        };

        if (openDialog.ShowDialog() == true)
        {
            try
            {
                var flow = await _flowRepository.LoadAsync(openDialog.FileName);
                CurrentFlow = flow;
                FlowName = flow.Name;
                TargetWindowTitle = flow.TargetWindowTitle ?? string.Empty;
                Steps.Clear();
                foreach (var step in flow.Steps)
                {
                    Steps.Add(step);
                }

                if (flow.ExecutionSource is RepeatExecutionSource repeatSource)
                {
                    UsesCsv = false;
                    RepeatCount = repeatSource.RepeatCount;
                }
                else
                {
                    UsesCsv = true;
                    RepeatCount = 1;
                }

                SelectedIndex = Steps.Count > 0 ? 0 : -1;
                StatusMessage = $"✓ Fluxo '{flow.Name}' carregado com sucesso!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Erro ao carregar fluxo: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    public void AddStep(string stepType)
    {
        Step newStep = stepType switch
        {
            "ActivateWindow" => new ActivateWindowStep { Label = "Ativar Janela" },
            "Wait" => new WaitStep { Milliseconds = 1000, Label = "Aguardar 1s" },
            "Click" => new ClickStep { X = 0, Y = 0, Label = "Clique", Origin = CoordinateOrigin.Screen },
            "TypeText" => new TypeTextStep { Template = "{{Coluna}}", Label = "Digitar Texto", Mode = TypeMode.DirectInput },
            "KeyPress" => new KeyPressStep { Keys = new List<string> { "Tab" }, Label = "Pressionar Tecla" },
            _ => new WaitStep { Label = "Novo Step" }
        };

        Steps.Add(newStep);
        SelectedIndex = Steps.Count - 1;
        StatusMessage = $"➕ Step '{stepType}' adicionado";
    }

    [RelayCommand]
    public void RemoveStep()
    {
        if (SelectedIndex < 0)
            return;

        Steps.RemoveAt(SelectedIndex);
        SelectedIndex = Math.Min(SelectedIndex, Steps.Count - 1);
        StatusMessage = "🗑️ Step removido";
    }

    [RelayCommand]
    public void MoveStepUp()
    {
        if (SelectedIndex <= 0) return;

        Steps.Move(SelectedIndex, SelectedIndex - 1);
        SelectedIndex -= 1;
        StatusMessage = "⬆️ Step movido para cima";
    }

    [RelayCommand]
    public void MoveStepDown()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Steps.Count - 1) return;

        Steps.Move(SelectedIndex, SelectedIndex + 1);
        SelectedIndex += 1;
        StatusMessage = "⬇️ Step movido para baixo";
    }

    [RelayCommand]
    public async Task CapturePosition()
    {
        if (SelectedStep is not ClickStep clickStep)
        {
            StatusMessage = "Selecione um step de tipo 'Click' primeiro";
            return;
        }

        StatusMessage = "🎯 Clique em qualquer lugar da tela para capturar a posição (15s timeout)...";

        // Minimizar a janela principal para o usuário conseguir clicar no sistema alvo
        var mainWindow = Application.Current.MainWindow;
        var originalState = mainWindow?.WindowState;
        if (mainWindow != null)
        {
            mainWindow.WindowState = WindowState.Minimized;
        }

        try
        {
            var position = await _positionCaptureService.CaptureNextClickAsync(TimeSpan.FromSeconds(15), CancellationToken.None);

            if (position.HasValue)
            {
                clickStep.X = position.Value.X;
                clickStep.Y = position.Value.Y;
                clickStep.Origin = CoordinateOrigin.Screen;
                StatusMessage = $"✓ Posição capturada: X={clickStep.X}, Y={clickStep.Y} (absoluta na tela)";
            }
            else
            {
                StatusMessage = "⏱️ Timeout — nenhum clique detectado nos últimos 15s";
            }
        }
        finally
        {
            // Restaurar a janela
            if (mainWindow != null && originalState.HasValue)
            {
                mainWindow.WindowState = originalState.Value;
                mainWindow.Activate();
            }
        }
    }

    [RelayCommand]
    public void InsertPlaceholder(string? columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName) || SelectedStep is not TypeTextStep typeTextStep)
        {
            StatusMessage = "⚠️ Selecione um TypeText step e escolha uma coluna";
            return;
        }

        typeTextStep.Template += $"{{{columnName}}}";
        StatusMessage = $"✓ Placeholder {{{columnName}}} inserido";
    }

    public void SetAvailableColumns(List<string> columns)
    {
        _availableColumns = columns;
    }

    public List<string> GetAvailableColumns()
    {
        return _availableColumns;
    }

    public List<Step> GetSteps()
    {
        return Steps.ToList();
    }
}
