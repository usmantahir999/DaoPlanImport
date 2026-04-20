using System;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DaoPlanImport.Data;
using DaoPlanImport.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VSDiagnostics;

namespace DaoPlanImport.Benchmarks;
[CPUUsageDiagnoser]
public class LigaCsvImportBenchmark
{
    private IImportService _importService;
    private IServiceProvider _serviceProvider;
    private string _testCsvPath;
    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
        services.AddSingleton(configuration);
        services.AddDbContext<DaoPlanDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
        services.AddLogging(config =>
        {
            config.ClearProviders();
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Warning);
            config.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
            config.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });
        services.AddScoped<IZipExtractorService, ZipExtractorService>();
        services.AddScoped<IFileProcessorService, FileProcessorService>();
        services.AddScoped<ICsvReaderService, CsvReaderService>();
        services.AddScoped<IDataMapperService, DataMapperService>();
        services.AddScoped<IDatabaseService, DatabaseService>();
        var baseFolderPath = configuration["Settings:BaseFolderPath"] ?? "West_12_till_19";
        var extractedFolderPath = configuration["Settings:ExtractedFolderPath"] ?? "Extracted";
        var batchSize = int.Parse(configuration["Settings:BatchSize"] ?? "500");
        var deleteExtractedAfterProcessing = bool.Parse(configuration["Settings:DeleteExtractedAfterProcessing"] ?? "false");
        var appBaseDirectory = AppContext.BaseDirectory;
        var projectRoot = Directory.GetParent(appBaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName ?? AppContext.BaseDirectory;
        var absoluteBaseFolderPath = Path.IsPathRooted(baseFolderPath) ? baseFolderPath : Path.Combine(projectRoot, baseFolderPath);
        var absoluteExtractedFolderPath = Path.IsPathRooted(extractedFolderPath) ? extractedFolderPath : Path.Combine(projectRoot, extractedFolderPath);
        absoluteBaseFolderPath = Path.GetFullPath(absoluteBaseFolderPath);
        absoluteExtractedFolderPath = Path.GetFullPath(absoluteExtractedFolderPath);
        services.AddScoped<IImportService>(provider => new ImportService(provider.GetRequiredService<IZipExtractorService>(), provider.GetRequiredService<IFileProcessorService>(), provider.GetRequiredService<ICsvReaderService>(), provider.GetRequiredService<IDataMapperService>(), provider.GetRequiredService<IDatabaseService>(), provider.GetRequiredService<ILogger<ImportService>>(), absoluteBaseFolderPath, absoluteExtractedFolderPath, batchSize, deleteExtractedAfterProcessing));
        _serviceProvider = services.BuildServiceProvider();
        _importService = _serviceProvider.GetRequiredService<IImportService>();
    }

    [Benchmark]
    public async Task ImportLigaData()
    {
        await _importService.ProcessAllDataAsync();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_serviceProvider as IDisposable)?.Dispose();
    }
}