using DaoPlanImport.Data;
using DaoPlanImport.Services;
using DaoPlanImport.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
});

// Add services
services.AddScoped<IZipExtractorService, ZipExtractorService>();
services.AddScoped<IFileProcessorService, FileProcessorService>();
services.AddScoped<ICsvReaderService, CsvReaderService>();
services.AddScoped<IDataMapperService, DataMapperService>();
services.AddScoped<IDatabaseService, DatabaseService>();
services.AddScoped<MigrationHelper>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

// Get configuration values
var baseFolderPath = configuration["Settings:BaseFolderPath"] ?? "./West_12_till_19/";
var extractedFolderPath = configuration["Settings:ExtractedFolderPath"] ?? "./Extracted/";
var batchSize = int.Parse(configuration["Settings:BatchSize"] ?? "500");
var deleteExtractedAfterProcessing = bool.Parse(configuration["Settings:DeleteExtractedAfterProcessing"] ?? "false");

// Register ImportService with configuration
services.AddScoped<IImportService>(provider =>
    new ImportService(
        provider.GetRequiredService<IZipExtractorService>(),
        provider.GetRequiredService<IFileProcessorService>(),
        provider.GetRequiredService<ICsvReaderService>(),
        provider.GetRequiredService<IDataMapperService>(),
        provider.GetRequiredService<IDatabaseService>(),
        provider.GetRequiredService<ILogger<ImportService>>(),
        baseFolderPath,
        extractedFolderPath,
        batchSize,
        deleteExtractedAfterProcessing
    )
);

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

        // Run import service (optional - comment out if only want to set up database)
        // var importService = scope.ServiceProvider.GetRequiredService<IImportService>();
        // await importService.ProcessAllDataAsync();

        logger.LogInformation("Application completed successfully");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during application execution");
        Environment.Exit(1);
    }
}
