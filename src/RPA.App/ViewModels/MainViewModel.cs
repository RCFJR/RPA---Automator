using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPA.Automation.Recording;
using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;
using RPA.Execution.Services;
using RPA.Persistence.Services;

namespace RPA.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private CsvLoaderViewModel csvLoaderViewModel;

    [ObservableProperty]
    private FlowEditorViewModel flowEditorViewModel;

    [ObservableProperty]
    private ExecutionViewModel executionViewModel;

    [ObservableProperty]
    private ConditionalEditorViewModel conditionalEditorViewModel;

    [ObservableProperty]
    private int selectedTabIndex = 0;

    public MainViewModel()
    {
        // Criar dependências
        var csvLoader = new CsvLoader();
        var placeholderResolver = new PlaceholderResolver();
        var flowRepository = new FlowRepository();
        var snapshotRepository = new SnapshotRepository();
        var inputSimulator = new InputSimulatorService();
        var windowActivator = new WindowActivatorService();
        var screenCaptureService = new ScreenCaptureService();
        var imageComparisonService = new ImageComparisonService();
        var stepExecutor = new StepExecutor(inputSimulator, windowActivator, placeholderResolver, screenCaptureService, imageComparisonService, snapshotRepository);
        var flowExecutionEngine = new FlowExecutionEngine(stepExecutor);
        var positionCaptureService = new PositionCaptureService();
        var recorderService = new RecorderService();

        // Criar ViewModels
        CsvLoaderViewModel = new CsvLoaderViewModel(csvLoader);
        FlowEditorViewModel = new FlowEditorViewModel(flowRepository, placeholderResolver, positionCaptureService, recorderService);
        ExecutionViewModel = new ExecutionViewModel(flowExecutionEngine, stepExecutor);
        ConditionalEditorViewModel = new ConditionalEditorViewModel(snapshotRepository, screenCaptureService);
    }

    [RelayCommand]
    public void ExecuteFlow()
    {
        var flow = FlowEditorViewModel.CurrentFlow;

        if (flow.Steps.Count == 0)
        {
            ExecutionViewModel.StatusMessage = "Adicione steps ao fluxo!";
            return;
        }

        if (FlowEditorViewModel.UsesCsv)
        {
            var rows = CsvLoaderViewModel.GetLoadedRows();
            if (rows.Count == 0)
            {
                ExecutionViewModel.StatusMessage = "Carregue um CSV primeiro!";
                return;
            }

            var headers = CsvLoaderViewModel.GetHeaders();
            FlowEditorViewModel.SetAvailableColumns(headers);

            // Ir para aba de execução
            SelectedTabIndex = 2;

            // Executar com CSV
            ExecutionViewModel.ExecuteCommand.Execute((flow, rows));
        }
        else
        {
            if (FlowEditorViewModel.RepeatCount <= 0)
            {
                ExecutionViewModel.StatusMessage = "Quantidade de repetições deve ser maior que 0!";
                return;
            }

            // Ir para aba de execução
            SelectedTabIndex = 2;

            // Executar com repetições
            ExecutionViewModel.ExecuteCommand.Execute((flow, FlowEditorViewModel.RepeatCount));
        }
    }

    [RelayCommand]
    public void GoToEditorTab()
    {
        SelectedTabIndex = 0;
    }

    [RelayCommand]
    public void GoToCsvTab()
    {
        SelectedTabIndex = 1;
    }

    [RelayCommand]
    public void GoToExecutionTab()
    {
        SelectedTabIndex = 2;
    }
}
