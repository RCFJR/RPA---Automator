using System.Text.Json;
using RPA.Core.Models;

namespace RPA.Persistence.Services;

public class FlowRepository : IFlowRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<Flow> LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Flow file not found: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        var flow = JsonSerializer.Deserialize<Flow>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Flow from JSON.");

        return flow;
    }

    public async Task SaveAsync(Flow flow, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(flow, JsonOptions);
        await File.WriteAllTextAsync(filePath, json);
    }
}
