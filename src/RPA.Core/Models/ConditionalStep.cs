namespace RPA.Core.Models;

public record ConditionalStep : Step
{
    private ImageComparisonCondition? _condition;
    private List<Step> _thenSteps = new();
    private List<Step>? _elseSteps;

    public ImageComparisonCondition? Condition
    {
        get => _condition;
        set => SetField(ref _condition, value);
    }

    public List<Step> ThenSteps
    {
        get => _thenSteps;
        set => SetField(ref _thenSteps, value);
    }

    public List<Step>? ElseSteps
    {
        get => _elseSteps;
        set => SetField(ref _elseSteps, value);
    }

    public override string Summary
    {
        get
        {
            var snapshotId = Condition?.SnapshotId.ToString("N").Substring(0, 8) ?? "???";
            var threshold = Condition?.SimilarityThreshold ?? 100;
            return $"Se imagem '{snapshotId}' ({threshold}%) → {ThenSteps.Count} ações, senão → {ElseSteps?.Count ?? 0} ações";
        }
    }
}
