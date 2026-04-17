// This file contains examples for extending the DaoPlanImport application
// All examples work with the single-table Liga schema

using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using Microsoft.EntityFrameworkCore;

namespace DaoPlanImport.Examples;

/// <summary>
/// Example 1: Data Validation
/// Add validation to prevent invalid data from being inserted
/// </summary>
public class ValidationExample
{
    public static bool ValidateLiga(Liga liga)
    {
        if (liga == null)
            return false;

        if (string.IsNullOrWhiteSpace(liga.ABONNR) || liga.ABONNR.Length > 50)
            return false;

        if (string.IsNullOrWhiteSpace(liga.GADENAVN) || liga.GADENAVN.Length > 200)
            return false;

        if (liga.ImportDate == default)
            return false;

        return true;
    }
}

/// <summary>
/// Example 2: Duplicate Detection by File Segregation
/// Find and manage records from specific files
/// </summary>
public class DuplicateDetectionExample
{
    public static async Task<bool> RecordsExistFromFileAsync(DaoPlanDbContext context, string fileName)
    {
        return await context.Ligas
            .AnyAsync(l => l.FileName == fileName);
    }

    public static async Task<int> GetRecordCountByFileAsync(DaoPlanDbContext context, string fileName)
    {
        return await context.Ligas
            .Where(l => l.FileName == fileName)
            .CountAsync();
    }

    public static async Task<List<Liga>> GetRecordsByFileAsync(DaoPlanDbContext context, string fileName)
    {
        return await context.Ligas
            .Where(l => l.FileName == fileName)
            .ToListAsync();
    }
}

/// <summary>
/// Example 3: Data Querying and Reporting
/// Query imported data for reporting by file segregation
/// </summary>
public class QueryingAndReportingExample
{
    public static async Task<dynamic> GetImportSummaryAsync(DaoPlanDbContext context)
    {
        var totalRecords = await context.Ligas.CountAsync();
        var importedFiles = await context.Ligas
            .Select(l => l.FileName)
            .Distinct()
            .CountAsync();
        
        var latestImport = await context.Ligas
            .OrderByDescending(l => l.ImportDate)
            .Select(l => l.ImportDate)
            .FirstOrDefaultAsync();

        return new
        {
            TotalRecords = totalRecords,
            TotalImportedFiles = importedFiles,
            LatestImportDate = latestImport
        };
    }

    public static async Task<List<dynamic>> GetAddressesByPostalCodeAsync(DaoPlanDbContext context, string postalCode)
    {
        var result = await context.Ligas
            .Where(l => l.POSTNR == postalCode)
            .Select(l => new
            {
                l.GADENAVN,
                l.HUSNR,
                l.ETAGE,
                l.POSTDISTRIKT,
                l.ABONNAVN,
                l.FileName,
                l.ImportDate
            })
            .ToListAsync();

        return result.Cast<dynamic>().ToList();
    }

    public static async Task<List<dynamic>> SearchBySubscriberAsync(DaoPlanDbContext context, string subscriberNumber)
    {
        var result = await context.Ligas
            .Where(l => (l.ABONNR ?? "").Contains(subscriberNumber))
            .Select(l => new
            {
                l.ABONNR,
                l.ABONNAVN,
                l.GADENAVN,
                l.HUSNR,
                l.POSTNR,
                l.PRODUKTNR,
                l.FileName,
                l.ImportDate
            })
            .ToListAsync();

        return result.Cast<dynamic>().ToList();
    }
}

/// <summary>
/// Example 4: Cleanup and Maintenance
/// Remove old or corrupted data
/// </summary>
public class CleanupExample
{
    public static async Task<int> DeleteOldImportsAsync(DaoPlanDbContext context, DateTime cutoffDate)
    {
        var oldRecords = await context.Ligas
            .Where(l => l.ImportDate < cutoffDate)
            .ToListAsync();

        context.Ligas.RemoveRange(oldRecords);
        var deletedCount = await context.SaveChangesAsync();

        return deletedCount;
    }

    public static async Task<int> DeleteRecordsByFileAsync(DaoPlanDbContext context, string fileName)
    {
        var recordsToDelete = await context.Ligas
            .Where(l => l.FileName == fileName)
            .ToListAsync();

        context.Ligas.RemoveRange(recordsToDelete);
        var deletedCount = await context.SaveChangesAsync();

        return deletedCount;
    }

    public static async Task<int> DeleteCorruptedRecordsAsync(DaoPlanDbContext context)
    {
        var corruptedRecords = await context.Ligas
            .Where(l => (l.GADENAVN == null || l.GADENAVN == "") || 
                       (l.HUSNR == null || l.HUSNR == ""))
            .ToListAsync();

        context.Ligas.RemoveRange(corruptedRecords);
        var deletedCount = await context.SaveChangesAsync();

        return deletedCount;
    }
}

/// <summary>
/// Example 5: Incremental Import Tracking
/// Track and manage incremental imports by file
/// </summary>
public class IncrementalImportExample
{
    public static class ImportTracker
    {
        private static readonly string TrackingFilePath = "import_history.json";

        public static void RecordImport(string zipFileName, DateTime importDate, int recordCount)
        {
            var history = LoadImportHistory();
            var key = zipFileName;
            
            if (history.ContainsKey(key))
            {
                history[key] = new ImportRecord
                {
                    LastImportDate = importDate,
                    RecordCount = recordCount,
                    ImportCount = history[key].ImportCount + 1
                };
            }
            else
            {
                history[key] = new ImportRecord
                {
                    LastImportDate = importDate,
                    RecordCount = recordCount,
                    ImportCount = 1
                };
            }
            
            SaveImportHistory(history);
        }

        public static bool IsAlreadyImported(string zipFileName)
        {
            var history = LoadImportHistory();
            return history.ContainsKey(zipFileName);
        }

        public static ImportRecord? GetImportHistory(string zipFileName)
        {
            var history = LoadImportHistory();
            return history.TryGetValue(zipFileName, out var record) ? record : null;
        }

        public class ImportRecord
        {
            public DateTime LastImportDate { get; set; }
            public int RecordCount { get; set; }
            public int ImportCount { get; set; }
        }

        private static Dictionary<string, ImportRecord> LoadImportHistory()
        {
            if (!System.IO.File.Exists(TrackingFilePath))
                return new Dictionary<string, ImportRecord>();

            try
            {
                var json = System.IO.File.ReadAllText(TrackingFilePath);
                var options = new System.Text.Json.JsonSerializerOptions();
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ImportRecord>>(json, options)
                    ?? new Dictionary<string, ImportRecord>();
            }
            catch
            {
                return new Dictionary<string, ImportRecord>();
            }
        }

        private static void SaveImportHistory(Dictionary<string, ImportRecord> history)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(history,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(TrackingFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving import history: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Example 6: Batch Operations with Progress Tracking
/// Enhanced batching with progress reporting
/// </summary>
public class ProgressTrackingExample
{
    public interface IProgressReporter
    {
        void ReportProgress(int processed, int total, string status);
    }

    public class ConsoleProgressReporter : IProgressReporter
    {
        public void ReportProgress(int processed, int total, string status)
        {
            var percentage = total > 0 ? (processed * 100) / total : 0;
            Console.WriteLine($"[{percentage}%] {processed}/{total} - {status}");
        }
    }

    public static async Task InsertWithProgressAsync(
        DaoPlanDbContext context,
        IEnumerable<Liga> items,
        int batchSize,
        IProgressReporter? reporter = null)
    {
        var itemList = items.ToList();
        var total = itemList.Count;

        for (int i = 0; i < itemList.Count; i += batchSize)
        {
            var batch = itemList.Skip(i).Take(batchSize).ToList();
            context.Ligas.AddRange(batch);
            await context.SaveChangesAsync();

            var processed = Math.Min(i + batchSize, total);
            reporter?.ReportProgress(processed, total, $"Inserted batch of {batch.Count}");
        }
    }
}

/// <summary>
/// Example 7: Performance Analysis
/// Measure and analyze import performance
/// </summary>
public class PerformanceAnalysisExample
{
    public class ImportMetricsData
    {
        public string? FileName { get; set; }
        public DateTime ImportStartTime { get; set; }
        public DateTime ImportEndTime { get; set; }
        public int ProcessedRecords { get; set; }
        public int InsertedRecords { get; set; }
        public int ErrorsCount { get; set; }

        public TimeSpan ImportDuration => ImportEndTime - ImportStartTime;
        public double RecordsPerSecond => ImportDuration.TotalSeconds > 0 ? ProcessedRecords / ImportDuration.TotalSeconds : 0;
        public double SuccessRate => ProcessedRecords > 0 ? (double)InsertedRecords / ProcessedRecords * 100 : 0;
    }

    public static ImportMetricsData CreateMetrics(string fileName, int processed, int inserted, int errors)
    {
        return new ImportMetricsData
        {
            FileName = fileName,
            ImportStartTime = DateTime.UtcNow,
            ImportEndTime = DateTime.UtcNow,
            ProcessedRecords = processed,
            InsertedRecords = inserted,
            ErrorsCount = errors
        };
    }

    public static void PrintMetrics(ImportMetricsData metrics)
    {
        Console.WriteLine($"File: {metrics.FileName}");
        Console.WriteLine($"Duration: {metrics.ImportDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Records: {metrics.ProcessedRecords} processed, {metrics.InsertedRecords} inserted");
        Console.WriteLine($"Throughput: {metrics.RecordsPerSecond:F2} records/sec");
        Console.WriteLine($"Success Rate: {metrics.SuccessRate:F2}%");
        Console.WriteLine($"Errors: {metrics.ErrorsCount}");
        Console.WriteLine();
    }
}

/// <summary>
/// Example 8: Error Recovery and Retry Logic
/// Implement retry logic for transient failures
/// </summary>
public class ErrorRecoveryExample
{
    public static async Task<T?> RetryOperationAsync<T>(
        Func<Task<T?>> operation,
        int maxRetries = 3,
        TimeSpan? delay = null)
    {
        delay ??= TimeSpan.FromSeconds(1);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}. Retrying...");
                await Task.Delay(delay.Value);
            }
        }

        return default;
    }

    public static class RetryPolicies
    {
        public static async Task<bool> InsertWithRetryAsync(
            DaoPlanDbContext context,
            Liga entity,
            int maxRetries = 3)
        {
            var result = await RetryOperationAsync(async () =>
            {
                context.Ligas.Add(entity);
                await context.SaveChangesAsync();
                return true;
            }, maxRetries);

            return result == true;
        }
    }
}

/// <summary>
/// Example 9: Statistical Analysis
/// Analyze imported data for insights
/// </summary>
public class StatisticalAnalysisExample
{
    public static async Task<dynamic> GetAddressStatisticsAsync(DaoPlanDbContext context)
    {
        var totalAddresses = await context.Ligas.CountAsync();
        
        var addressesByPostalCode = await context.Ligas
            .GroupBy(l => l.POSTNR)
            .Select(g => new { PostalCode = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var addressesByProduct = await context.Ligas
            .GroupBy(l => l.PRODUKTNR)
            .Select(g => new { Product = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        return new
        {
            TotalAddresses = totalAddresses,
            AddressesByPostalCode = addressesByPostalCode,
            AddressesByProduct = addressesByProduct
        };
    }

    public static async Task<dynamic> GetSubscriberStatisticsAsync(DaoPlanDbContext context)
    {
        var totalSubscribers = await context.Ligas
            .Select(l => l.ABONNR)
            .Distinct()
            .CountAsync();

        var subscribersPerFile = await context.Ligas
            .GroupBy(l => l.FileName)
            .Select(g => new
            {
                FileName = g.Key,
                UniqueSubscribers = g.Select(x => x.ABONNR).Distinct().Count()
            })
            .ToListAsync();

        return new
        {
            TotalUniqueSubscribers = totalSubscribers,
            SubscribersPerFile = subscribersPerFile
        };
    }
}

/// <summary>
/// Example 10: Data Export and Backup
/// Export data for backup or external processing
/// </summary>
public class DataExportExample
{
    public static async Task ExportAllDataAsync(DaoPlanDbContext context, string exportPath)
    {
        var records = await context.Ligas.ToListAsync();

        using var writer = new System.IO.StreamWriter(exportPath);
        
        // Write header
        await writer.WriteLineAsync("Id,FileName,ImportDate,ABONNR,ABONNAVN,GADENAVN,HUSNR,ETAGE,POSTNR,PRODUKTNR,BARCODE");

        // Write data
        foreach (var record in records)
        {
            var line = $"{record.Id},\"{record.FileName}\",{record.ImportDate:yyyy-MM-dd HH:mm:ss},\"{record.ABONNR}\",\"{record.ABONNAVN}\",\"{record.GADENAVN}\",\"{record.HUSNR}\",\"{record.ETAGE}\",\"{record.POSTNR}\",\"{record.PRODUKTNR}\",\"{record.BARCODE}\"";
            await writer.WriteLineAsync(line);
        }
    }

    public static async Task ExportByFileNameAsync(DaoPlanDbContext context, string fileName, string exportPath)
    {
        var records = await context.Ligas
            .Where(l => l.FileName == fileName)
            .ToListAsync();

        using var writer = new System.IO.StreamWriter(exportPath);
        
        await writer.WriteLineAsync("Id,FileName,ImportDate,ABONNR,ABONNAVN,GADENAVN,HUSNR,ETAGE,POSTNR,PRODUKTNR,BARCODE");

        foreach (var record in records)
        {
            var line = $"{record.Id},\"{record.FileName}\",{record.ImportDate:yyyy-MM-dd HH:mm:ss},\"{record.ABONNR}\",\"{record.ABONNAVN}\",\"{record.GADENAVN}\",\"{record.HUSNR}\",\"{record.ETAGE}\",\"{record.POSTNR}\",\"{record.PRODUKTNR}\",\"{record.BARCODE}\"";
            await writer.WriteLineAsync(line);
        }
    }

    public static async Task ExportByPostalCodeAsync(DaoPlanDbContext context, string postalCode, string exportPath)
    {
        var records = await context.Ligas
            .Where(l => l.POSTNR == postalCode)
            .OrderBy(l => l.GADENAVN)
            .ThenBy(l => l.HUSNR)
            .ToListAsync();

        using var writer = new System.IO.StreamWriter(exportPath);
        
        await writer.WriteLineAsync("ABONNR,ABONNAVN,GADENAVN,HUSNR,ETAGE,PRODUKTNR,BARCODE,ImportDate");

        foreach (var record in records)
        {
            var line = $"\"{record.ABONNR}\",\"{record.ABONNAVN}\",\"{record.GADENAVN}\",\"{record.HUSNR}\",\"{record.ETAGE}\",\"{record.PRODUKTNR}\",\"{record.BARCODE}\",{record.ImportDate:yyyy-MM-dd HH:mm:ss}";
            await writer.WriteLineAsync(line);
        }
    }
}
