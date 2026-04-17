using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface IFileProcessorService
{
    (string? LigaFile, List<string> SupportingFiles) IdentifyFiles(string extractedFolderPath);
}

public class FileProcessorService : IFileProcessorService
{
    private readonly ILogger<FileProcessorService> _logger;

    public FileProcessorService(ILogger<FileProcessorService> logger)
    {
        _logger = logger;
    }

    public (string? LigaFile, List<string> SupportingFiles) IdentifyFiles(string extractedFolderPath)
    {
        string? ligaFile = null;
        var supportingFiles = new List<string>();

        if (!Directory.Exists(extractedFolderPath))
        {
            _logger.LogWarning("Extracted folder not found: {ExtractedFolderPath}", extractedFolderPath);
            return (null, supportingFiles);
        }

        var csvFiles = Directory.GetFiles(extractedFolderPath, "*.csv");
        _logger.LogInformation("Found {CsvFileCount} CSV files in {ExtractedFolderPath}", csvFiles.Length, extractedFolderPath);

        foreach (var csvFile in csvFiles)
        {
            var fileName = Path.GetFileName(csvFile);

            if (fileName.Contains("_Liga", StringComparison.OrdinalIgnoreCase))
            {
                ligaFile = csvFile;
                _logger.LogInformation("Identified main Liga file: {FileName}", fileName);
            }
            else
            {
                supportingFiles.Add(csvFile);
                _logger.LogDebug("Identified supporting file: {FileName}", fileName);
            }
        }

        if (ligaFile == null)
        {
            _logger.LogWarning("No Liga file found in folder: {ExtractedFolderPath}", extractedFolderPath);
        }

        return (ligaFile, supportingFiles);
    }
}
