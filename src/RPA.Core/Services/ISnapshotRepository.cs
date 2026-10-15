using RPA.Core.Models;

namespace RPA.Core.Services;

public interface ISnapshotRepository
{
    Task<ImageSnapshot?> GetAsync(Guid id);
    Task<List<ImageSnapshot>> GetAllAsync();
    Task SaveAsync(ImageSnapshot snapshot);
    Task DeleteAsync(Guid id);
}
