namespace RPA.Automation.Services;

public interface IImageComparisonService
{
    Task<bool> CompareAsync(byte[] image1, byte[] image2, int similarityThreshold = 100);
    Task<int> GetSimilarityPercentageAsync(byte[] image1, byte[] image2);
}
