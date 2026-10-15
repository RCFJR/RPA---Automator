using System.Drawing;

namespace RPA.Automation.Services;

public class ImageComparisonService : IImageComparisonService
{
    public async Task<bool> CompareAsync(byte[] image1, byte[] image2, int similarityThreshold = 100)
    {
        var similarity = await GetSimilarityPercentageAsync(image1, image2);
        return similarity >= similarityThreshold;
    }

    public async Task<int> GetSimilarityPercentageAsync(byte[] image1, byte[] image2)
    {
        return await Task.Run(() =>
        {
            using (var bmp1 = new Bitmap(new MemoryStream(image1)))
            using (var bmp2 = new Bitmap(new MemoryStream(image2)))
            {
                if (bmp1.Width != bmp2.Width || bmp1.Height != bmp2.Height)
                    return 0;

                int matchingPixels = 0;
                int totalPixels = bmp1.Width * bmp1.Height;

                for (int y = 0; y < bmp1.Height; y++)
                {
                    for (int x = 0; x < bmp1.Width; x++)
                    {
                        if (bmp1.GetPixel(x, y) == bmp2.GetPixel(x, y))
                            matchingPixels++;
                    }
                }

                return (int)((matchingPixels * 100.0) / totalPixels);
            }
        });
    }
}
