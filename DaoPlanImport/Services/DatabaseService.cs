using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DaoPlanImport.Services;

public interface IDatabaseService
{
    Task InsertBatchAsync<T>(IEnumerable<T> entities, int batchSize) where T : class;
    Task SaveChangesAsync();
}

public class DatabaseService : IDatabaseService
{
    private readonly DaoPlanDbContext _context;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(DaoPlanDbContext context, ILogger<DatabaseService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task InsertBatchAsync<T>(IEnumerable<T> entities, int batchSize) where T : class
    {
        var batch = new List<T>(batchSize);
        var totalCount = 0;

        foreach (var entity in entities)
        {
            batch.Add(entity);

            if (batch.Count >= batchSize)
            {
                try
                {
                    totalCount += batch.Count;
                    _context.Set<T>().AddRange(batch);
                    await _context.SaveChangesAsync();
                    batch.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error inserting batch of {EntityType} records", typeof(T).Name);
                    throw;
                }
            }
        }

        // Insert remaining records
        if (batch.Count > 0)
        {
            try
            {
                totalCount += batch.Count;
                _context.Set<T>().AddRange(batch);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting final batch of {EntityType} records", typeof(T).Name);
                throw;
            }
        }
    }

    public async Task SaveChangesAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Changes saved to database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving changes to database");
            throw;
        }
    }
}
