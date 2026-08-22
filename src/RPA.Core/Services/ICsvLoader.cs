using RPA.Core.Models;

namespace RPA.Core.Services;

public interface ICsvLoader
{
    Task<(List<string> Headers, List<CsvRow> Rows)> LoadAsync(string filePath);
    Task<(List<string> Headers, List<CsvRow> Rows)> LoadAsync(Stream stream);
}
