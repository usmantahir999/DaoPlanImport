using System.Globalization;
using CsvHelper;
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

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        await csv.ReadAsync();
        csv.ReadHeader();

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
                        record[header] = csv.GetField(header);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading field '{Header}' from CSV file: {FilePath}", header, filePath);
                        record[header] = null;
                    }
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
