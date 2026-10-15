using RPA.Core.Models;
using System.Drawing;
using System.Drawing.Imaging;

namespace RPA.Automation.Services;

public class ScreenCaptureService : IScreenCaptureService
{
    public Task<byte[]> CaptureRegionAsync(ImageRegion region)
    {
        try
        {
            var bounds = new Rectangle(region.X, region.Y, region.Width, region.Height);
            var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
            
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }

            using (var ms = new MemoryStream())
            {
                bitmap.Save(ms, ImageFormat.Png);
                bitmap.Dispose();
                return Task.FromResult(ms.ToArray());
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao capturar região: {ex.Message}", ex);
        }
    }
}
