using DaoPlanImport.Entities;
using Microsoft.Extensions.Logging;
using System.Xml.Schema;

namespace DaoPlanImport.Services;

public interface IImportService
{
    Task ProcessAllDataAsync();
}

public class ImportService : IImportService
{
    private readonly IZipExtractorService _zipExtractor;
    private readonly IFileProcessorService _fileProcessor;
    private readonly ICsvReaderService _csvReader;
    private readonly IDataMapperService _dataMapper;
    private readonly IDatabaseService _databaseService;
    private readonly ILogger<ImportService> _logger;
    private readonly string _baseFolderPath;
    private readonly string _extractedFolderPath;
    private readonly int _batchSize;
    private readonly bool _deleteExtractedAfterProcessing;

    public ImportService(
        IZipExtractorService zipExtractor,
        IFileProcessorService fileProcessor,
        ICsvReaderService csvReader,
        IDataMapperService dataMapper,
        IDatabaseService databaseService,
        ILogger<ImportService> logger,
        string baseFolderPath,
        string extractedFolderPath,
        int batchSize,
        bool deleteExtractedAfterProcessing)
    {
        _zipExtractor = zipExtractor;
        _fileProcessor = fileProcessor;
        _csvReader = csvReader;
        _dataMapper = dataMapper;
        _databaseService = databaseService;
        _logger = logger;
        _baseFolderPath = baseFolderPath;
        _extractedFolderPath = extractedFolderPath;
        _batchSize = batchSize;
        _deleteExtractedAfterProcessing = deleteExtractedAfterProcessing;
    }

    public async Task ProcessAllDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting data import process");
            _logger.LogInformation("Base folder: {BaseFolderPath}", _baseFolderPath);
            _logger.LogInformation("Extracted folder: {ExtractedFolderPath}", _extractedFolderPath);
            _logger.LogInformation("Batch size: {BatchSize}", _batchSize);

            // Extract all ZIP files
            var extractedPaths = await _zipExtractor.ExtractAllZipsAsync(_baseFolderPath, _extractedFolderPath);
            _logger.LogInformation("Extraction complete. Found {ExtractedCount} folders to process", extractedPaths.Count());

            // Process each extracted folder
            foreach (var extractedPath in extractedPaths)
            {
                await ProcessExtractedFolderAsync(extractedPath);

                if (_deleteExtractedAfterProcessing)
                {
                    try
                    {
                        Directory.Delete(extractedPath, recursive: true);
                        _logger.LogInformation("Deleted extracted folder: {ExtractedPath}", extractedPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete extracted folder: {ExtractedPath}", extractedPath);
                    }
                }
            }

            _logger.LogInformation("Data import process completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data import process");
            throw;
        }
    }

    private async Task ProcessExtractedFolderAsync(string extractedPath)
    {
        _logger.LogInformation("Processing extracted folder: {ExtractedPath}", extractedPath);

        // Get all CSV files from the extracted folder
        var csvFiles = Directory.GetFiles(extractedPath, "*.csv");
        
        if (csvFiles.Length == 0)
        {
            _logger.LogWarning("No CSV files found in {ExtractedPath}. Skipping folder.", extractedPath);
            return;
        }

        _logger.LogInformation("Found {CsvFileCount} CSV files to process", csvFiles.Length);

        try
        {
            // Process all CSV files and dump them into Liga table with FileName segregation
            foreach (var csvFile in csvFiles.Where(x=>x.Contains("_Liga")))
            {
                await ProcessCsvFileAsync(csvFile);
            }
            
            _logger.LogInformation("Folder processing completed: {ExtractedPath}", extractedPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing folder: {ExtractedPath}", extractedPath);
        }
    }

    private async Task ProcessCsvFileAsync(string csvFilePath)
    {
        var fileName = Path.GetFileName(csvFilePath);
        _logger.LogInformation("Processing CSV file: {FileName}", fileName);

        var ligaRecords = new List<Liga>();
        var recordCount = 0;

        await foreach (var record in _csvReader.ReadCsvAsync(csvFilePath))
            {
                try
                {
                    var liga = _dataMapper.MapToLiga(record, fileName);
                    if (liga == null)
                    {
                        continue;
                    }

                    ligaRecords.Add(liga);
                    recordCount++;

                    // Insert batch when reaching batch size
                    if (ligaRecords.Count >= _batchSize)
                    {
                        await _databaseService.InsertBatchAsync(ligaRecords, _batchSize);
                        ligaRecords.Clear();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing record from file: {FileName}", fileName);
                }
            }

        // Insert remaining records
        if (ligaRecords.Count > 0)
        {
            await _databaseService.InsertBatchAsync(ligaRecords, _batchSize);
        }

        _logger.LogInformation("Processed {RecordCount} records from {FileName}", recordCount, fileName);
    }
}
