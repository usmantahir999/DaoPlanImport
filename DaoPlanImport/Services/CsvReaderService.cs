using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface ICsvReaderService
{
    IAsyncEnumerable<Dictionary<string, object?>> ReadCsvAsync(string filePath);
}

public class CsvReaderService : ICsvReaderService
{
    private readonly ILogger<CsvReaderService> _logger;

    public CsvReaderService(ILogger<CsvReaderService> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<Dictionary<string, object?>> ReadCsvAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("CSV file not found: {FilePath}", filePath);
            yield break;
        }

        using var reader = new StreamReader(filePath, Encoding.GetEncoding("ISO-8859-1"));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        };
        using var csv = new CsvReader(reader, config);
        
        await csv.ReadAsync();
        csv.ReadHeader();

        var headerRecord = csv.HeaderRecord;
        if (headerRecord != null)
        {
            _logger.LogInformation("CSV Headers: {Headers}", string.Join(", ", headerRecord.Select(h => $"'{h}'")));
        }

        var rowCount = 0;
        while (await csv.ReadAsync())
        {
            Dictionary<string, object?> record;
            try
            {
                record = new Dictionary<string, object?>();
                foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
                {
                    try
                    {
                        // Trim header to handle whitespace in column names
                        var trimmedHeader = header?.Trim() ?? string.Empty;
                        var fieldValue = csv.GetField(header);
                        record[trimmedHeader] = fieldValue;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading field '{Header}' from CSV file: {FilePath}", header, filePath);
                        record[header?.Trim() ?? string.Empty] = null;
                    }
                }
                rowCount++;
                if (rowCount == 1)
                {
                    _logger.LogInformation("First record values: {Values}", string.Join(", ", record.Select(kvp => $"{kvp.Key}='{kvp.Value}'")));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading row from CSV file: {FilePath}", filePath);
                continue;
            }

            yield return record;
        }
    }
}
