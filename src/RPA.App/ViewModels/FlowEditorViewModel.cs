using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RPA.App.Models;
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
    private readonly IRecorderService _recorderService;
    private RecordingOverlayWindow? _recordingOverlay;
    private RecordingOverlayViewModel? _recordingOverlayViewModel;
    private WindowState? _preRecordingWindowState;

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
        "KeyPress",
        "SetVariable",
        "ShellCommand"
    };

    [ObservableProperty]
    private bool usesCsv = true;

    [ObservableProperty]
    private int repeatCount = 1;

    [ObservableProperty]
    private bool isRecording = false;

    [ObservableProperty]
    private Step? selectedSubStep;

    [ObservableProperty]
    private int selectedSubStepIndex = -1;

    [ObservableProperty]
    private ObservableCollection<GlobalVariable> globalVariables = new();

    [ObservableProperty]
    private string newVariableName = string.Empty;

    [ObservableProperty]
    private string newVariableValue = string.Empty;

    private List<string> _availableColumns = new();

    public Step? EditingStep => SelectedSubStep ?? SelectedStep;

    public FlowEditorViewModel(IFlowRepository flowRepository, IPlaceholderResolver placeholderResolver, IPositionCaptureService positionCaptureService, IRecorderService recorderService)
    {
        _flowRepository = flowRepository ?? throw new ArgumentNullException(nameof(flowRepository));
        _placeholderResolver = placeholderResolver ?? throw new ArgumentNullException(nameof(placeholderResolver));
        _positionCaptureService = positionCaptureService ?? throw new ArgumentNullException(nameof(positionCaptureService));
        _recorderService = recorderService ?? throw new ArgumentNullException(nameof(recorderService));
        CreateNewFlow();
    }

    partial void OnSelectedIndexChanged(int value)
    {
        SelectedStep = (value >= 0 && value < Steps.Count) ? Steps[value] : null;
        SelectedSubStepIndex = -1;
    }

    partial void OnSelectedSubStepIndexChanged(int value)
    {
        var subSteps = (SelectedStep as RecordedSequenceStep)?.RecordedSteps;
        SelectedSubStep = (subSteps != null && value >= 0 && value < subSteps.Count) ? subSteps[value] : null;
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
            ExecutionSource = new RepeatExecutionSource(1),
            GlobalVariables = new Dictionary<string, string>()
        };
        FlowName = CurrentFlow.Name;
        TargetWindowTitle = CurrentFlow.TargetWindowTitle ?? string.Empty;
        Steps.Clear();
        GlobalVariables.Clear();
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

            var globalVariablesDict = new Dictionary<string, string>();
            foreach (var gv in GlobalVariables)
            {
                globalVariablesDict[gv.Name] = gv.Value;
            }

            CurrentFlow = CurrentFlow with
            {
                Name = FlowName,
                TargetWindowTitle = TargetWindowTitle,
                Steps = Steps.ToList(),
                ExecutionSource = executionSource,
                GlobalVariables = globalVariablesDict
            };

            var fileName = $"{FlowName.Replace(" ", "_")}.rpaflow.json";
            var filePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RPA", fileName);

            await _flowRepository.SaveAsync(CurrentFlow, filePath);
            StatusMessage = $"✓ Fluxo '{FlowName}' salvo com sucesso!";
            System.Windows.MessageBox.Show($"Fluxo '{FlowName}' foi salvo com sucesso em:\n{filePath}", "Fluxo Salvo", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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
                GlobalVariables.Clear();

                foreach (var step in flow.Steps)
                {
                    Steps.Add(step);
                }

                foreach (var gv in flow.GlobalVariables)
                {
                    GlobalVariables.Add(new Models.GlobalVariable
                    {
                        Name = gv.Key,
                        Value = gv.Value
                    });
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
            "SetVariable" => new SetVariableStep { Label = "Atribuir Variável", VariableName = "variavel", Value = "valor" },
            "ShellCommand" => new ShellCommandStep { Label = "Comando Shell", DefaultDelayMs = 1000, Commands = new() },
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
    public void AddGlobalVariable()
    {
        if (string.IsNullOrWhiteSpace(NewVariableName))
        {
            StatusMessage = "Nome da variável é obrigatório";
            return;
        }

        GlobalVariables.Add(new Models.GlobalVariable
        {
            Name = NewVariableName,
            Value = NewVariableValue
        });

        NewVariableName = string.Empty;
        NewVariableValue = string.Empty;
        StatusMessage = "✅ Variável global adicionada";
    }

    [RelayCommand]
    public void RemoveGlobalVariable(Models.GlobalVariable? variable)
    {
        if (variable == null)
            return;

        GlobalVariables.Remove(variable);
        StatusMessage = "🗑️ Variável global removida";
    }

    [RelayCommand]
    public async Task CapturePosition()
    {
        if (EditingStep is not ClickStep clickStep)
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
        if (string.IsNullOrWhiteSpace(columnName) || EditingStep is not TypeTextStep typeTextStep)
        {
            StatusMessage = "⚠️ Selecione um TypeText step e escolha uma coluna";
            return;
        }

        typeTextStep.Template += $"{{{columnName}}}";
        StatusMessage = $"✓ Placeholder {{{columnName}}} inserido";
    }

    [RelayCommand]
    public void StartRecording()
    {
        if (IsRecording) return;

        var mainWindow = Application.Current.MainWindow;
        _preRecordingWindowState = mainWindow?.WindowState;
        if (mainWindow != null)
            mainWindow.WindowState = WindowState.Minimized;

        _recordingOverlayViewModel = new RecordingOverlayViewModel();
        _recordingOverlayViewModel.StopRequested += async (_, _) => await StopRecordingInternal();
        _recordingOverlay = new RecordingOverlayWindow(_recordingOverlayViewModel);
        _recordingOverlay.Show();

        _recorderService.StepCaptured += OnStepCaptured;
        _recorderService.Start();
        IsRecording = true;
        StatusMessage = "🔴 Gravando... interaja com a janela alvo e clique em Parar quando terminar.";
    }

    [RelayCommand]
    public async Task RequestStopRecording()
    {
        await StopRecordingInternal();
    }

    private async Task StopRecordingInternal()
    {
        if (!IsRecording) return;

        _recorderService.StepCaptured -= OnStepCaptured;
        var sequenceStep = await _recorderService.StopAsync();
        IsRecording = false;
        _recordingOverlay?.Close();
        _recordingOverlay = null;
        _recordingOverlayViewModel = null;

        var mainWindow = Application.Current.MainWindow;
        if (mainWindow != null && _preRecordingWindowState.HasValue)
        {
            mainWindow.WindowState = _preRecordingWindowState.Value;
            mainWindow.Activate();
        }

        if (sequenceStep.RecordedSteps.Count > 0)
        {
            Steps.Add(sequenceStep);
            SelectedIndex = Steps.Count - 1;
            StatusMessage = $"✓ Sequência gravada com {sequenceStep.RecordedSteps.Count} passo(s)";
        }
        else
        {
            StatusMessage = "⚠️ Nenhuma ação foi capturada durante a gravação.";
        }
    }

    private void OnStepCaptured(object? sender, Step step)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_recordingOverlayViewModel != null)
                _recordingOverlayViewModel.ActionsCaptured++;
        });
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
