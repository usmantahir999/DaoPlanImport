using DaoPlanImport.Data;
using DaoPlanImport.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

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
        var entityList = entities.ToList();
        if (entityList.Count == 0)
            return;

        // For Liga entities, use SqlBulkCopy (much faster)
        if (typeof(T) == typeof(Liga))
        {
            await BulkInsertLigaAsync((IEnumerable<Liga>)entityList);
        }
        else
        {
            // Fallback to EF Core for other types
            await TraditionalInsertAsync(entityList, batchSize);
        }
    }

    private async Task BulkInsertLigaAsync(IEnumerable<Liga> ligas)
    {
        try
        {
            var dataTable = ConvertToDataTable(ligas);
            
            var connection = _context.Database.GetDbConnection() as SqlConnection;
            if (connection == null)
                throw new InvalidOperationException("Connection must be SqlConnection for bulk copy");

            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using (var bulkCopy = new SqlBulkCopy(connection))
            {
                bulkCopy.DestinationTableName = "Ligas";
                bulkCopy.BulkCopyTimeout = 0; // No timeout
                bulkCopy.BatchSize = 10000;

                // Map DataTable columns to database columns
                foreach (DataColumn column in dataTable.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(dataTable);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk inserting Liga records");
            throw;
        }
    }

    private DataTable ConvertToDataTable<T>(IEnumerable<T> entities) where T : class
    {
        var dataTable = new DataTable(typeof(T).Name);
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Create columns
        foreach (var prop in properties)
        {
            var columnType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
            dataTable.Columns.Add(prop.Name, columnType);
        }

        // Add rows
        foreach (var entity in entities)
        {
            var values = properties.Select(prop => prop.GetValue(entity) ?? DBNull.Value).ToArray();
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }

    private async Task TraditionalInsertAsync<T>(IEnumerable<T> entities, int batchSize) where T : class
    {
        var batch = new List<T>(batchSize);

        foreach (var entity in entities)
        {
            batch.Add(entity);

            if (batch.Count >= batchSize)
            {
                try
                {
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
