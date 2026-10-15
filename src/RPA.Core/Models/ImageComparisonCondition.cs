namespace RPA.Core.Models;

public record ImageComparisonCondition(
    Guid SnapshotId,
    ImageRegion Region,
    int SimilarityThreshold = 100);
