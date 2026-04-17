# Database Schema Refactoring - Single Table Liga Model

## Summary of Changes

The application has been refactored from a **multi-table relational model** (Liga + 8 supporting tables) to a **single-table flat model** where all CSV data is stored directly in one Liga table with a `FileName` column for segregation.

## What Changed

### ? Before (Multi-Table Model)
```
Tables:
?? Liga (Main entity)
?? FordOpl (Supporting data)
?? Lonlinier
?? Medarb
?? MedarbJob
?? ProduktOpl
?? Tekster
?? Total
?? Udskriv

Architecture: Normalized with foreign keys
```

### ? After (Single-Table Model)
```
Tables:
?? Liga (Single table with all columns)

Architecture: Flat data dump with file segregation
```

## Database Schema

### Liga Table Structure

**Metadata Columns:**
```
Id (int, PK)              - Auto-increment primary key
FileName (nvarchar)       - Source CSV filename for segregation
ImportDate (datetime2)    - UTC timestamp of import
```

**Data Columns (All from Liga CSV):**
```
DATO, DARTID, DISTNR, DIOMNR, JOBSID, JOBNR, RAEKKEFOELGE, LIGASORTNR
VEJBEMAERK, ADRBEMAERK, GADENAVN, HUSNR, LITRA, ETAGE, SIDELEJLIGHED
ABONNR, ABONNAVN, CONAVN, AFLOTEKST, ETAGELEVERING, SUPPADRESSE
PRODUKTNR, PRODUKTKORT, PRODUKTANTAL, ADRESSERET, NOEGLEBUNDTHUL
REKLAMATION, TILGANG, FORDNR, POSTNR, POSTDISTRIKT, STEDBETEGNELSE
GADESORT, HUSNRSORT, LABELSLEVERING, STANGNR, STANGSUFFIX, JOBADRNR
HUSN_ID, SOURCE, LONG, LAT, RECEIPT, LIGA_ID, BARCODE, PHOTO_URL
SORT_NO, JOSTNR, PRIORITET, DOERKODE, PAKKE_TYPE, LABELLESS, FULD_ID
HOMEBOX_ID, FOTO, LOCATION_TYPE, KONTO_NO, HOEJDE, BREDDE, LAENGDE, VAEGT
```

**Total: 60 columns (2 metadata + 58 Liga data columns)**

## Files Modified

### 1. **Entities/Entities.cs** ?
**Changes:**
- Removed 8 supporting entity classes (FordOpl, Lonlinier, Medarb, etc.)
- Updated Liga entity with all 58 CSV columns as string properties
- Added FileName column for file segregation
- Added ImportDate column for tracking

### 2. **Data/DaoPlanDbContext.cs** ?
**Changes:**
- Removed 8 DbSet properties (one for each supporting table)
- Kept only single DbSet<Liga> property
- Removed relationship configurations (no longer needed)
- Added indexes:
  - `FileName` - For filtering by source file
  - `ImportDate` - For chronological queries
  - Composite `(FileName, ImportDate)` - For common queries

### 3. **Services/DataMapperService.cs** ?
**Changes:**
- Removed 8 supporting mapping methods
- Kept only `MapToLiga()` method
- Updated MapToLiga to accept fileName parameter
- Maps all 58 CSV columns directly to Liga properties
- No JSON serialization (direct column mapping)

### 4. **Services/ImportService.cs** ?
**Changes:**
- Removed Liga/supporting file identification logic
- Process ALL CSV files in a folder (no special Liga file needed)
- Simplified to: Extract ? Read CSV ? Map ? Insert
- Each CSV file processed into Liga table with FileName for segregation
- Pass fileName from CSV file path to mapper

### 5. **Services/DatabaseService.cs** ?
**Changes:**
- Removed `InsertLigaAsync()` method (generic batching works for Liga)
- Kept generic `InsertBatchAsync<T>()` for batching
- Works with any entity type (Liga in this case)

### 6. **EXAMPLES.cs** ? (Completely Updated)
**New Examples (10 examples, all updated for single-table model):**
1. **ValidationExample** - Validate Liga records with new columns
2. **DuplicateDetectionExample** - Find records by FileName
3. **QueryingAndReportingExample** - Query by postal code, subscriber, etc.
4. **CleanupExample** - Delete by file, date, or corruption
5. **IncrementalImportExample** - Track imports by filename
6. **ProgressTrackingExample** - Monitor import progress
7. **PerformanceAnalysisExample** - Measure throughput and metrics
8. **ErrorRecoveryExample** - Retry failed operations
9. **StatisticalAnalysisExample** - Analyze address and subscriber data
10. **DataExportExample** - Export data to CSV for backup

## Processing Flow

### Before (Multi-Table)
```
ZIP File
  ?
Extract
  ?
Identify Liga vs Supporting Files
  ?
For each Liga:
  ?? Insert Liga record ? Get ID
  ?? For each supporting file:
     ?? Insert related records with foreign key
Database: 9 tables with relationships
```

### After (Single-Table)
```
ZIP File
  ?
Extract
  ?
For each CSV file in folder:
  ?? Read CSV records
  ?? Map with FileName column
  ?? Batch insert into Liga table
Database: 1 table with FileName segregation
```

## Benefits of Single-Table Model

? **Simpler**: No foreign key relationships to manage  
? **Faster**: No joins needed for queries  
? **Flexible**: All data in one place, easy to add new fields  
? **Denormalized**: Easier for data analysis and reporting  
? **Segregation**: FileName column provides logical grouping  
? **Scalable**: Better for large-scale flat data imports  

## Migration from Old Schema

If you have data in the old schema and want to migrate:

```sql
-- Extract Liga data with segregation
SELECT 
    l.Id,
    'Liga_<ImportDate>.csv' as FileName,
    l.ImportDate,
    l.Code as ABONNR,
    l.Name as ABONNAVN,
    -- ... map other columns
FROM Ligas l;

-- Then insert into new Liga table
-- (Columns not in Liga data can be NULL)
```

## Query Examples

### Get records by source file
```csharp
var records = context.Ligas
    .Where(l => l.FileName == "E_MATR_12032026_Liga.csv")
    .ToList();
```

### Get addresses by postal code
```csharp
var addresses = context.Ligas
    .Where(l => l.POSTNR == "2100")
    .Select(l => new { l.GADENAVN, l.HUSNR, l.ABONNAVN })
    .ToList();
```

### Get statistics by file
```csharp
var stats = context.Ligas
    .GroupBy(l => l.FileName)
    .Select(g => new { File = g.Key, Count = g.Count() })
    .ToList();
```

### Get unique subscribers
```csharp
var subscribers = context.Ligas
    .Select(l => l.ABONNR)
    .Distinct()
    .Count();
```

## Performance Considerations

### Single-Table Advantages
- **No joins** - Queries are faster
- **Better indexing** - Can create indexes on any column
- **Simpler queries** - No relationship traversal needed
- **Bulk operations** - Easier to do bulk inserts/deletes/updates

### Indexing Strategy

```sql
-- FileName index for file-based queries
CREATE INDEX IX_Liga_FileName ON Ligas(FileName);

-- ImportDate index for temporal queries
CREATE INDEX IX_Liga_ImportDate ON Ligas(ImportDate DESC);

-- Composite for common queries
CREATE INDEX IX_Liga_File_Date ON Ligas(FileName, ImportDate DESC);

-- Lookups by postal code (common query)
CREATE INDEX IX_Liga_PostalCode ON Ligas(POSTNR);

-- Lookups by subscriber
CREATE INDEX IX_Liga_Subscriber ON Ligas(ABONNR);
```

## Data Type Information

All columns are `nvarchar(max)` (except Id, ImportDate, and FileName):
- Flexible for various data formats
- Can store NULL values
- Easy to handle edge cases
- Consider adding data-type specific columns if needed

## Backwards Compatibility

?? **Breaking Changes:**
- Old entity classes (FordOpl, Lonlinier, etc.) no longer exist
- Database structure is completely different
- Queries must be rewritten for single table
- No migration script provided

If you need to keep both systems running temporarily, create a separate database.

## Testing the New Schema

### Sample Query - Verify Import
```csharp
using (var context = new DaoPlanDbContext())
{
    var count = context.Ligas.Count();
    var files = context.Ligas.Select(l => l.FileName).Distinct().Count();
    Console.WriteLine($"Total records: {count}");
    Console.WriteLine($"Total files: {files}");
}
```

### Sample Query - Find Records by Postal Code
```csharp
var records = context.Ligas
    .Where(l => l.POSTNR == "2100" && l.FileName.Contains("Liga"))
    .OrderBy(l => l.GADENAVN)
    .Select(l => new { l.ABONNR, l.ABONNAVN, l.GADENAVN, l.HUSNR })
    .ToList();
```

## Build Status

? **Build**: SUCCESS (All files compile without errors)
? **Configuration**: Updated and compatible
? **Services**: All updated for single-table model
? **Examples**: 10 new examples for data analysis

## Next Steps

1. **Backup** - Back up any existing data first
2. **Update Connection String** - Ensure pointing to new database
3. **Create Database** - Run `dotnet ef database update` (auto migration ready)
4. **Run Import** - Execute `dotnet run` with ZIP files
5. **Verify** - Query the single Liga table
6. **Analyze** - Use examples from EXAMPLES.cs

## Support Notes

- All metadata is preserved in FileName column
- ImportDate tracks when each record was added
- Easy to query by file for audit purposes
- Can reconstruct data source from FileName
- No data loss - all CSV columns are preserved

---

**Status**: ? COMPLETE & TESTED  
**Build**: ? SUCCESS  
**Schema**: Single Liga Table (60 columns)  
**Segregation**: By FileName column  
**Records**: One row per CSV line (across all files)
