using RPA.Core.Models;
using System.Text.Json;

namespace RPA.Core.Services;

public class SnapshotRepository : ISnapshotRepository
{
    private readonly string _snapshotsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "snapshots");
    private readonly Dictionary<Guid, ImageSnapshot> _cache = new();

    public SnapshotRepository()
    {
        Directory.CreateDirectory(_snapshotsDir);
        _ = LoadAllAsync();
    }

    public async Task<ImageSnapshot?> GetAsync(Guid id)
    {
        if (_cache.TryGetValue(id, out var snapshot))
            return snapshot;

        var path = Path.Combine(_snapshotsDir, $"{id}.png");
        if (!File.Exists(path))
            return null;

        var metadataPath = Path.Combine(_snapshotsDir, $"{id}.json");
        if (!File.Exists(metadataPath))
            return null;

        var json = await File.ReadAllTextAsync(metadataPath);
        var metadata = JsonSerializer.Deserialize<SnapshotMetadata>(json);
        var imageData = await File.ReadAllBytesAsync(path);

        var result = new ImageSnapshot(id, metadata!.Name, imageData, metadata.Width, metadata.Height, metadata.CapturedAt);
        _cache[id] = result;
        return result;
    }

    public async Task<List<ImageSnapshot>> GetAllAsync()
    {
        var result = new List<ImageSnapshot>(_cache.Values);
        return await Task.FromResult(result);
    }

    public async Task SaveAsync(ImageSnapshot snapshot)
    {
        var imagePath = Path.Combine(_snapshotsDir, $"{snapshot.Id}.png");
        var metadataPath = Path.Combine(_snapshotsDir, $"{snapshot.Id}.json");

        await File.WriteAllBytesAsync(imagePath, snapshot.ImageData);

        var metadata = new SnapshotMetadata
        {
            Name = snapshot.Name,
            Width = snapshot.Width,
            Height = snapshot.Height,
            CapturedAt = snapshot.CapturedAt
        };

        var json = JsonSerializer.Serialize(metadata);
        await File.WriteAllTextAsync(metadataPath, json);

        _cache[snapshot.Id] = snapshot;
    }

    public async Task DeleteAsync(Guid id)
    {
        var imagePath = Path.Combine(_snapshotsDir, $"{id}.png");
        var metadataPath = Path.Combine(_snapshotsDir, $"{id}.json");

        if (File.Exists(imagePath))
            File.Delete(imagePath);
        if (File.Exists(metadataPath))
            File.Delete(metadataPath);

        _cache.Remove(id);
        await Task.CompletedTask;
    }

    private async Task LoadAllAsync()
    {
        var jsonFiles = Directory.GetFiles(_snapshotsDir, "*.json");
        foreach (var jsonPath in jsonFiles)
        {
            var json = await File.ReadAllTextAsync(jsonPath);
            var metadata = JsonSerializer.Deserialize<SnapshotMetadata>(json);
            var idStr = Path.GetFileNameWithoutExtension(jsonPath);

            if (Guid.TryParse(idStr, out var id))
            {
                var imagePath = Path.Combine(_snapshotsDir, $"{id}.png");
                if (File.Exists(imagePath))
                {
                    var imageData = await File.ReadAllBytesAsync(imagePath);
                    var snapshot = new ImageSnapshot(id, metadata!.Name, imageData, metadata.Width, metadata.Height, metadata.CapturedAt);
                    _cache[id] = snapshot;
                }
            }
        }
    }

    private class SnapshotMetadata
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime CapturedAt { get; set; }
    }
}
