using DaoPlanImport.Data;
using DaoPlanImport.Services;
using DaoPlanImport.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Setup dependency injection
var services = new ServiceCollection();

// Add configuration
services.AddSingleton(configuration);

// Add DbContext
services.AddDbContext<DaoPlanDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
);

// Add logging
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
    // Disable verbose EF Core SQL logging
    config.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    config.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
});

// Add services
services.AddScoped<IZipExtractorService, ZipExtractorService>();
services.AddScoped<IFileProcessorService, FileProcessorService>();
services.AddScoped<ICsvReaderService, CsvReaderService>();
services.AddScoped<IDataMapperService, DataMapperService>();
services.AddScoped<IDatabaseService, DatabaseService>();
services.AddScoped<MigrationHelper>();

// Get configuration values
var baseFolderPath = configuration["Settings:BaseFolderPath"] ?? "West_12_till_19";
var extractedFolderPath = configuration["Settings:ExtractedFolderPath"] ?? "Extracted";
var batchSize = int.Parse(configuration["Settings:BatchSize"] ?? "500");
var deleteExtractedAfterProcessing = bool.Parse(configuration["Settings:DeleteExtractedAfterProcessing"] ?? "false");

// Convert relative paths to absolute paths based on solution root
// AppContext.BaseDirectory = bin\Debug\net8.0\
// We need to go up 5 levels to get to solution root where West_12_till_19 is located
var appBaseDirectory = AppContext.BaseDirectory;
var projectRoot = Directory.GetParent(appBaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName 
    ?? AppContext.BaseDirectory;

var absoluteBaseFolderPath = Path.IsPathRooted(baseFolderPath) 
    ? baseFolderPath 
    : Path.Combine(projectRoot, baseFolderPath);
var absoluteExtractedFolderPath = Path.IsPathRooted(extractedFolderPath)
    ? extractedFolderPath
    : Path.Combine(projectRoot, extractedFolderPath);

// Normalize paths
absoluteBaseFolderPath = Path.GetFullPath(absoluteBaseFolderPath);
absoluteExtractedFolderPath = Path.GetFullPath(absoluteExtractedFolderPath);

// Register ImportService with configuration BEFORE building service provider
services.AddScoped<IImportService>(provider =>
    new ImportService(
        provider.GetRequiredService<IZipExtractorService>(),
        provider.GetRequiredService<IFileProcessorService>(),
        provider.GetRequiredService<ICsvReaderService>(),
        provider.GetRequiredService<IDataMapperService>(),
        provider.GetRequiredService<IDatabaseService>(),
        provider.GetRequiredService<ILogger<ImportService>>(),
        absoluteBaseFolderPath,
        absoluteExtractedFolderPath,
        batchSize,
        deleteExtractedAfterProcessing
    )
);

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Create a new scope for initialization and import
using (var scope = serviceProvider.CreateScope())
{
    try
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Application started");

        // Initialize database with migrations
        var migrationHelper = scope.ServiceProvider.GetRequiredService<MigrationHelper>();
        logger.LogInformation("Initializing database with migrations");
        
        var dbState = await migrationHelper.InitializeDatabaseAsync();
        logger.LogInformation("Database State: {DbState}", dbState);

        // Apply any pending migrations
        if (dbState.PendingMigrationCount > 0)
        {
            logger.LogInformation("Applying {MigrationCount} pending migration(s)", dbState.PendingMigrationCount);
            await migrationHelper.MigrateAsync();
        }

        // Verify Liga table
        var tableVerified = await migrationHelper.VerifyLigaTableAsync();
        if (!tableVerified)
        {
            logger.LogError("Failed to verify Liga table");
            Environment.Exit(1);
        }

        logger.LogInformation("Database initialized successfully");

        // Run import service
        logger.LogInformation("Starting CSV import service");
        var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        Stopwatch stopwatch = Stopwatch.StartNew();
        await importService.ProcessAllDataAsync();
        stopwatch.Stop();       
        Console.WriteLine($"Total processing time: {stopwatch.Elapsed.TotalSeconds} seconds");

        logger.LogInformation("Application completed successfully");
        Console.ReadLine();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during application execution");
        Environment.Exit(1);
    }
}
