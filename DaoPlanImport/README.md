# DAO Plan Import Application

A comprehensive C# application using Entity Framework Core to process ZIP files and import CSV data into a SQL Server database.

## Overview

This application is designed to:
1. Extract ZIP files from a base folder
2. Process CSV files from extracted folders
3. Map CSV data to database entities
4. Insert data into SQL Server with proper batching and error handling
5. Maintain referential integrity between Liga and supporting data

## Architecture

### Project Structure

```
DaoPlanImport/
??? Program.cs                  # Application entry point
??? appsettings.json            # Configuration file
??? DaoPlanImport.csproj        # Project file with NuGet dependencies
??? Entities/
?   ??? Entities.cs             # All database entity models
??? Data/
?   ??? DaoPlanDbContext.cs     # Entity Framework DbContext
??? Services/
    ??? ZipExtractorService.cs   # Handles ZIP extraction
    ??? FileProcessorService.cs  # Identifies Liga and supporting files
    ??? CsvReaderService.cs      # Reads and parses CSV files
    ??? DataMapperService.cs     # Maps CSV records to entities
    ??? DatabaseService.cs       # Handles database operations with batching
    ??? ImportService.cs         # Orchestrates the entire import process
```

### Service Responsibilities

#### ZipExtractorService
- Iterates over ZIP files in the base folder
- Extracts to temporary folders
- Skips already extracted folders
- Handles extraction errors gracefully

#### FileProcessorService
- Identifies the main Liga CSV file (contains "_Liga" in filename)
- Collects all supporting files
- Logs missing Liga files

#### CsvReaderService
- Asynchronously reads CSV files using CsvHelper
- Returns records as dictionaries
- Handles per-field and per-row errors
- Memory-efficient for large files

#### DataMapperService
- Maps dictionary records to entity models
- Handles type conversions
- Serializes complex records as JSON for flexible storage

#### DatabaseService
- Manages DbContext operations
- Implements batch insertion for performance
- Configurable batch sizes
- Maintains transaction consistency

#### ImportService
- Orchestrates the entire workflow
- Processes one extracted folder at a time
- Manages Liga-supporting file relationships
- Handles cleanup of extracted folders (optional)

## Entity Models

### Liga (Main Entity)
- Represents the main data entity
- Has relationships with all supporting entities
- Foreign key relationships with cascade delete

### Supporting Entities
- **FordOpl**: Ford Operation data
- **Lonlinier**: Line data
- **Medarb**: Employee data
- **MedarbJob**: Employee Job data
- **ProduktOpl**: Product Operation data
- **Tekster**: Text data
- **Total**: Total summary data
- **Udskriv**: Print/Output data

All supporting entities include:
- `Id`: Primary key
- `LigaId`: Foreign key to Liga
- `Data`: JSON-serialized field data for flexibility

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DaoPlanDb;Trusted_Connection=true;"
  },
  "Settings": {
    "BaseFolderPath": "./West_12_till_19/",
    "ExtractedFolderPath": "./Extracted/",
    "BatchSize": 500,
    "DeleteExtractedAfterProcessing": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Configuration Options

| Option | Description | Default |
|--------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string | Local MSSQLLocalDB |
| `BaseFolderPath` | Root folder containing ZIP files | `./West_12_till_19/` |
| `ExtractedFolderPath` | Folder for extracted ZIP contents | `./Extracted/` |
| `BatchSize` | Number of records per database batch | 500 |
| `DeleteExtractedAfterProcessing` | Auto-delete extracted folders after processing | false |

## Usage

### Prerequisites
- .NET 8 or later
- SQL Server (LocalDB or Express)
- NuGet packages restored

### Running the Application

```bash
dotnet run
```

### Example Folder Structure

```
West_12_till_19/
??? E_MATR_12032026_175001.zip
?   ??? E_MATR_12032026_Liga.csv        (Main file)
?   ??? E_MATR_12032026_Ford_Opl.csv
?   ??? E_MATR_12032026_Lonlinier.csv
?   ??? E_MATR_12032026_medarb.csv
?   ??? E_MATR_12032026_medarb_job.csv
?   ??? E_MATR_12032026_Produkt_Opl.csv
?   ??? E_MATR_12032026_Tekster.csv
?   ??? E_MATR_12032026_Total.csv
?   ??? E_MATR_12032026_Udskriv.csv
??? E_MATR_13032026_175002.zip
?   ??? (similar structure)
??? ...
```

## Performance Considerations

### Scalability Features

1. **Batching**: Configurable batch sizes for database operations
   - Default: 500 records per batch
   - Adjust in `appsettings.json` for your hardware

2. **Async/Await**: All I/O operations are asynchronous
   - Non-blocking ZIP extraction
   - Non-blocking CSV reading
   - Non-blocking database operations

3. **Streaming CSV Reading**: Uses async enumeration
   - Processes large files without loading entire content into memory
   - Ideal for 100k+ row files

4. **One-at-a-Time Folder Processing**: Reduces memory footprint
   - Processes extracted folders sequentially
   - Optional cleanup after each folder

### Testing with Large Files

For files with 100k+ rows:
1. Increase `BatchSize` to 1000-2000 for better throughput
2. Monitor memory usage with large batch sizes
3. Ensure SQL Server connection pool settings are adequate
4. Consider connection string retry settings for large operations

## Error Handling

### Graceful Degradation

- **ZIP Extraction Errors**: Individual ZIP files are skipped, others continue
- **CSV Parsing Errors**: Individual rows/fields are logged, processing continues
- **Missing Liga Files**: Folder is skipped with warning log
- **Database Errors**: Batch operation fails with exception, but batch boundaries prevent data inconsistency

### Logging

The application uses Microsoft.Extensions.Logging:
- **Information**: High-level operation status
- **Debug**: Detailed file and record processing
- **Warning**: Non-critical issues (missing files, etc.)
- **Error**: Critical failures with stack traces

Configure logging level in `appsettings.json`:

```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "DaoPlanImport.Services": "Debug"
  }
}
```

## Database Initialization

The application automatically:
1. Creates the database if it doesn't exist
2. Creates all tables and relationships
3. Uses Entity Framework migrations (can be extended)

To manually create migrations:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore | 8.0.0 | ORM |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0 | SQL Server provider |
| CsvHelper | 30.0.0 | CSV parsing |
| Microsoft.Extensions.Configuration | 8.0.0 | Configuration management |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | Dependency injection |
| Microsoft.Extensions.Logging | 8.0.0 | Logging framework |
| Microsoft.Extensions.Logging.Console | 8.0.0 | Console logger provider |

## Future Enhancements

1. **Entity-Specific Mapping**: Replace JSON serialization with typed properties
2. **Partial File Resumption**: Track processing state to resume interrupted imports
3. **Data Validation**: Implement FluentValidation for business rules
4. **Duplicate Detection**: Prevent re-importing already processed files
5. **Concurrent Processing**: Process multiple folders in parallel (with caution)
6. **Export/Reporting**: Generate reports from imported data
7. **API Integration**: Expose import status via REST API
8. **Unit Tests**: Add comprehensive test coverage

## Troubleshooting

### Common Issues

**Issue**: Database connection fails
- **Solution**: Verify connection string in `appsettings.json`
- **Solution**: Ensure SQL Server service is running

**Issue**: ZIP extraction fails
- **Solution**: Check file permissions on base folder
- **Solution**: Ensure sufficient disk space in extracted folder path

**Issue**: CSV parsing errors
- **Solution**: Verify CSV file encoding (UTF-8 recommended)
- **Solution**: Check for inconsistent column counts

**Issue**: Out of memory with large files
- **Solution**: Reduce `BatchSize` in `appsettings.json`
- **Solution**: Increase application memory allocation
- **Solution**: Process folder by folder, not all at once

**Issue**: Slow import performance
- **Solution**: Increase `BatchSize` (test with 1000, 2000)
- **Solution**: Disable antivirus scanning during import
- **Solution**: Verify SQL Server is not I/O bound

## License

This project is provided as-is for educational and commercial use.
