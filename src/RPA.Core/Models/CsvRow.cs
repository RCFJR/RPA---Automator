namespace RPA.Core.Models;

public class CsvRow : Dictionary<string, string>
{
    public int RowIndex { get; set; }

    public string? GetValueOrDefault(string key) =>
        TryGetValue(key, out var value) ? value : null;
}
