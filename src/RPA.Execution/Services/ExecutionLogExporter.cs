using CsvHelper;
using RPA.Execution.Models;
using System.Globalization;

namespace RPA.Execution.Services;

public class ExecutionLogExporter : IExecutionLogExporter
{
    public async Task ExportToCsvAsync(List<ExecutionResult> results, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(filePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        await csv.WriteRecordsAsync(results);
        await writer.FlushAsync();
    }
}
