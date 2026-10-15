using RPA.Core.Models;

namespace RPA.Automation.Services;

public interface IScreenCaptureService
{
    Task<byte[]> CaptureRegionAsync(ImageRegion region);
}
