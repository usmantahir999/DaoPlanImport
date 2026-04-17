using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface IZipExtractorService
{
    Task<IEnumerable<string>> ExtractAllZipsAsync(string baseFolderPath, string extractedFolderPath);
}

public class ZipExtractorService : IZipExtractorService
{
    private readonly ILogger<ZipExtractorService> _logger;

    public ZipExtractorService(ILogger<ZipExtractorService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> ExtractAllZipsAsync(string baseFolderPath, string extractedFolderPath)
    {
        var extractedPaths = new List<string>();

        if (!Directory.Exists(baseFolderPath))
        {
            _logger.LogWarning("Base folder not found: {BaseFolderPath}", baseFolderPath);
            return extractedPaths;
        }

        var zipFiles = Directory.GetFiles(baseFolderPath, "*.zip");
        _logger.LogInformation("Found {ZipFileCount} ZIP files in {BaseFolderPath}", zipFiles.Length, baseFolderPath);

        Directory.CreateDirectory(extractedFolderPath);

        foreach (var zipFile in zipFiles)
        {
            try
            {
                var zipFileName = Path.GetFileNameWithoutExtension(zipFile);
                var extractPath = Path.Combine(extractedFolderPath, zipFileName);

                if (Directory.Exists(extractPath))
                {
                    _logger.LogInformation("Folder already extracted: {ExtractPath}. Skipping extraction.", extractPath);
                    extractedPaths.Add(extractPath);
                    continue;
                }

                _logger.LogInformation("Extracting ZIP file: {ZipFile} to {ExtractPath}", zipFile, extractPath);
                
                Directory.CreateDirectory(extractPath);
                await Task.Run(() => ZipFile.ExtractToDirectory(zipFile, extractPath, overwriteFiles: false));
                
                _logger.LogInformation("Successfully extracted: {ZipFile}", zipFile);
                extractedPaths.Add(extractPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting ZIP file: {ZipFile}", zipFile);
            }
        }

        return extractedPaths;
    }
}
