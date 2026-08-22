using CsvHelper;
using RPA.Core.Models;
using System.Globalization;

namespace RPA.Core.Services;

public class CsvLoader : ICsvLoader
{
    public async Task<(List<string> Headers, List<CsvRow> Rows)> LoadAsync(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return await LoadAsync(stream);
    }

    public async Task<(List<string> Headers, List<CsvRow> Rows)> LoadAsync(Stream stream)
    {
        return await Task.Run(() =>
        {
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            csv.Read();
            csv.ReadHeader();

            var headers = csv.HeaderRecord?.ToList() ?? new List<string>();
            var rows = new List<CsvRow>();

            int rowIndex = 0;
            while (csv.Read())
            {
                var row = new CsvRow { RowIndex = rowIndex };

                foreach (var header in headers)
                {
                    var value = csv.GetField(header) ?? string.Empty;
                    row[header] = value;
                }

                rows.Add(row);
                rowIndex++;
            }

            return (headers, rows);
        });
    }
}
