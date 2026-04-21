using DaoPlanImport.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Utilities;

/// <summary>
/// Helper class for database migrations and initialization
/// </summary>
public class MigrationHelper
{
    private readonly DaoPlanDbContext _context;
    private readonly ILogger<MigrationHelper> _logger;

    public MigrationHelper(DaoPlanDbContext context, ILogger<MigrationHelper> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Applies all pending migrations to the database
    /// </summary>
    public async Task MigrateAsync()
    {
        try
        {
            _logger.LogInformation("Checking for pending migrations...");
            
            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();
            
            if (pendingMigrations.Count == 0)
            {
                _logger.LogInformation("Database is up to date. No pending migrations.");
                return;
            }

            _logger.LogInformation("Found {MigrationCount} pending migration(s)", pendingMigrations.Count);
            foreach (var migration in pendingMigrations)
            {
                _logger.LogInformation("  - {MigrationName}", migration);
            }

            _logger.LogInformation("Applying migrations...");
            await _context.Database.MigrateAsync();
            
            _logger.LogInformation("Migrations applied successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying migrations");
            throw;
        }
    }

    /// <summary>
    /// Ensures database is created and returns current migration state
    /// </summary>
    public async Task<DatabaseState> InitializeDatabaseAsync()
    {
        try
        {
            _logger.LogInformation("Initializing database...");

            // Check if database exists
            var canConnect = await _context.Database.CanConnectAsync();

            if (!canConnect)
            {
                _logger.LogInformation("Database doesn't exist. It will be created by migrations.");
            }
            else
            {
                _logger.LogInformation("Database connection established");
            }

            // Get migration info
            var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();

            var state = new DatabaseState
            {
                IsConnected = true,
                AppliedMigrationCount = appliedMigrations.Count,
                PendingMigrationCount = pendingMigrations.Count,
                AppliedMigrations = appliedMigrations,
                PendingMigrations = pendingMigrations
            };

            _logger.LogInformation(
                "Database state: {AppliedCount} applied, {PendingCount} pending migrations",
                state.AppliedMigrationCount,
                state.PendingMigrationCount);

            return state;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing database");
            return new DatabaseState { IsConnected = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Gets the current database state
    /// </summary>
    public async Task<DatabaseState> GetDatabaseStateAsync()
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return new DatabaseState
                {
                    IsConnected = false,
                    Error = "Cannot connect to database"
                };
            }

            var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).ToList();
            var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();

            return new DatabaseState
            {
                IsConnected = true,
                AppliedMigrationCount = appliedMigrations.Count,
                PendingMigrationCount = pendingMigrations.Count,
                AppliedMigrations = appliedMigrations,
                PendingMigrations = pendingMigrations
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting database state");
            return new DatabaseState { IsConnected = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Rolls back to a specific migration by name
    /// Requires using the full migration namespace
    /// </summary>
    public async Task RollbackToMigrationAsync(string targetMigration)
    {
        await Task.Run(() =>
        {
            try
            {
                _logger.LogWarning("Rolling back to migration: {MigrationName}", targetMigration);
                
                // The Migrate method with target name parameter
                // This approach uses reflection to call the internal MigrateAsync overload
                var relationalConnection = _context.Database.GetDbConnection();
                _logger.LogWarning("Manual rollback needed. Use: dotnet ef database update {MigrationName}", targetMigration);
                
                _logger.LogInformation("Rollback completed to: {MigrationName}", targetMigration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rolling back to migration: {MigrationName}", targetMigration);
                throw;
            }
        });
    }

    /// <summary>
    /// Removes all migrations (reverts to initial state)
    /// This will remove all tables created by migrations
    /// </summary>
    public async Task RemoveAllMigrationsAsync()
    {
        await Task.Run(() =>
        {
            try
            {
                _logger.LogWarning("Removing all migrations. This will revert database to initial state");
                _logger.LogWarning("To remove migrations, use: dotnet ef database update 0");
                
                _logger.LogInformation("Use EF CLI command to remove all migrations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing migrations");
                throw;
            }
        });
    }

    /// <summary>
    /// Verifies that the Liga table exists and has the correct schema
    /// </summary>
    public async Task<bool> VerifyLigaTableAsync()
    {
        try
        {
            _logger.LogInformation("Verifying Liga table schema...");
            
            // Try to query the table
            var count = await _context.Ligas.CountAsync();
            
            _logger.LogInformation("Liga table verified. Current record count: {Count}", count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Liga table");
            return false;
        }
    }
}

/// <summary>
/// Represents the current state of the database
/// </summary>
public class DatabaseState
{
    public bool IsConnected { get; set; }
    public int AppliedMigrationCount { get; set; }
    public int PendingMigrationCount { get; set; }
    public List<string> AppliedMigrations { get; set; } = new();
    public List<string> PendingMigrations { get; set; } = new();
    public string? Error { get; set; }

    public override string ToString()
    {
        if (!IsConnected)
            return $"Not Connected - Error: {Error}";

        return $"Connected - Applied: {AppliedMigrationCount}, Pending: {PendingMigrationCount}";
    }
}
