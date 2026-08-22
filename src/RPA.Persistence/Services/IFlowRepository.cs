using RPA.Core.Models;

namespace RPA.Persistence.Services;

public interface IFlowRepository
{
    Task<Flow> LoadAsync(string filePath);
    Task SaveAsync(Flow flow, string filePath);
}
