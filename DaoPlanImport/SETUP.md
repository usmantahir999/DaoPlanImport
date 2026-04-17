# Setup Instructions

## Prerequisites

1. **Operating System**: Windows 10+ (or Linux/macOS with .NET 8 installed)
2. **.NET SDK**: Version 8.0 or later
3. **SQL Server**: LocalDB, Express, or Standard edition
4. **Visual Studio Code** or **Visual Studio 2022** (recommended)

## Installation Steps

### Step 1: Clone/Setup Project

```bash
# Navigate to your workspace
cd D:\CubivueRepository\DaoPlanImport\DaoPlanImport
```

### Step 2: Restore NuGet Packages

```bash
dotnet restore
```

### Step 3: Verify SQL Server Installation

#### Option A: Using SQL Server LocalDB (Recommended for Development)

LocalDB should be installed with Visual Studio. Verify:

```bash
sqllocaldb info mssqllocaldb
```

If not installed, you can install SQL Server Express from:
https://www.microsoft.com/en-us/sql-server/sql-server-downloads

#### Option B: Using SQL Server Express

Install SQL Server Express, then update connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=DaoPlanDb;Trusted_Connection=true;"
}
```

### Step 4: Prepare Folder Structure

Create the folder structure as specified in `appsettings.json`:

```bash
# From project root
mkdir "West_12_till_19"
mkdir "Extracted"
```

### Step 5: Build and Test

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

## Configuration Guide

### Update appsettings.json for Your Environment

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
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

### Connection String Examples

#### LocalDB (Development)
```
Server=(localdb)\mssqllocaldb;Database=DaoPlanDb;Trusted_Connection=true;
```

#### SQL Server Express
```
Server=.\SQLEXPRESS;Database=DaoPlanDb;Trusted_Connection=true;
```

#### Named Instance
```
Server=ServerName\InstanceName;Database=DaoPlanDb;Trusted_Connection=true;
```

#### SQL Authentication
```
Server=ServerAddress;Database=DaoPlanDb;User Id=sa;Password=YourPassword;
```

#### Azure SQL Database
```
Server=tcp:servername.database.windows.net,1433;Initial Catalog=DaoPlanDb;Persist Security Info=False;User ID=username;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Running the Application

### Basic Usage

```bash
cd DaoPlanImport
dotnet run
```

### Expected Output

```
Information: Application started
Information: Ensuring database is created
Information: Starting data import process
Information: Base folder: ./West_12_till_19/
Information: Extracted folder: ./Extracted/
Information: Batch size: 500
Information: Found 5 ZIP files in ./West_12_till_19/
Information: Extracting ZIP file: ./West_12_till_19/E_MATR_12032026_175001.zip to ./Extracted/E_MATR_12032026_175001
...
Information: Total 1000 Liga records processed
...
Information: Data import process completed successfully
Information: Application completed successfully
```

## Database Access

### View Database with SQL Server Management Studio

1. Open SQL Server Management Studio
2. Connect to: `(localdb)\mssqllocaldb`
3. Expand Databases
4. Find `DaoPlanDb`
5. Query tables:

```sql
-- View Liga table
SELECT TOP 10 * FROM Ligas ORDER BY ImportDate DESC;

-- Count records
SELECT 'Liga' AS TableName, COUNT(*) AS RecordCount FROM Ligas
UNION ALL
SELECT 'FordOpl', COUNT(*) FROM FordOpls
UNION ALL
SELECT 'Lonlinier', COUNT(*) FROM Lonnliniers
-- ... etc
```

### View Database with Visual Studio Code

Install the SQL Server extension for VS Code:
1. Open Extensions (Ctrl+Shift+X)
2. Search for "SQL Server"
3. Install "mssql"
4. Press Ctrl+Shift+P and type "MSSQL: Add Connection"
5. Follow the prompts to connect

## Customization Guide

### Adjusting Performance Settings

Edit `appsettings.json`:

```json
{
  "Settings": {
    "BatchSize": 1000  // For faster imports with large RAM
  }
}
```

### Changing Folder Paths

```json
{
  "Settings": {
    "BaseFolderPath": "C:/Data/DAO/West_12_till_19/",
    "ExtractedFolderPath": "C:/Data/DAO/Extracted/"
  }
}
```

### Enabling Debug Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "DaoPlanImport.Services": "Debug"
    }
  }
}
```

### Auto-Cleanup Extracted Folders

```json
{
  "Settings": {
    "DeleteExtractedAfterProcessing": true
  }
}
```

## Troubleshooting

### Issue: "Cannot connect to database"

1. Verify SQL Server is running:
   ```bash
   sqllocaldb start mssqllocaldb
   ```

2. Check connection string in `appsettings.json`

3. Test connection with SQL Server Management Studio

### Issue: "CSV file not found"

1. Verify `BaseFolderPath` exists
2. Check ZIP file names match pattern
3. Verify ZIP extraction permissions

### Issue: "Permission Denied" on folder creation

1. Ensure application has write permissions
2. Run with administrator privileges if necessary
3. Check folder ownership and permissions

### Issue: "Out of memory" with large files

1. Reduce `BatchSize` in `appsettings.json` (try 250 or 100)
2. Increase application memory: `dotnet run --max-memory 4gb`
3. Close other applications

### Issue: Slow performance

1. Check SQL Server is not doing backups
2. Increase `BatchSize` to 1000-2000
3. Monitor disk I/O and network latency
4. Verify indexes are created after initial import:

```sql
-- Create indexes for faster queries
CREATE INDEX IX_FordOpl_LigaId ON FordOpls(LigaId);
CREATE INDEX IX_Lonlinier_LigaId ON Lonnliniers(LigaId);
-- ... repeat for other tables
```

## Advanced Usage

### Manual Database Setup with Migrations

If you want to use EF Core migrations:

```bash
# Install EF Core CLI (if not installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update

# View migration history
dotnet ef migrations list
```

### Running Specific Data Processing

Modify `ImportService` to process only certain file types:

```csharp
// In ProcessSupportingFilesAsync, comment out unwanted file types
// to skip processing of specific CSV types
```

### Exporting Data

Query the database and export:

```sql
-- Export Liga records
SELECT * FROM Ligas
ORDER BY ImportDate DESC;

-- Export with supporting data
SELECT l.*, f.Data AS FordOplData, m.Data AS MedarbData
FROM Ligas l
LEFT JOIN FordOpls f ON l.Id = f.LigaId
LEFT JOIN Medarbs m ON l.Id = m.LigaId
WHERE l.ImportDate >= '2024-01-01';
```

## Performance Tuning

### For 100k+ Row Files

1. **Increase Batch Size**:
   ```json
   "BatchSize": 2000
   ```

2. **Disable Change Tracking for Read-Only Operations** (if applicable):
   - Add `AsNoTracking()` to queries if needed

3. **Use Connection Pooling**:
   ```
   "DefaultConnection": "Server=...;Pooling=true;Max Pool Size=100;"
   ```

4. **Monitor Performance**:
   ```sql
   -- Check table sizes
   EXEC sp_spaceused 'Ligas';
   EXEC sp_spaceused 'FordOpls';
   
   -- Monitor query plans
   SET STATISTICS IO ON;
   SELECT COUNT(*) FROM Ligas WHERE ImportDate > GETDATE()-1;
   SET STATISTICS IO OFF;
   ```

## Next Steps

1. Prepare your ZIP files in the `West_12_till_19` folder
2. Run the application: `dotnet run`
3. Monitor the console output for progress
4. Query the database to verify imported data
5. Adjust settings for optimal performance

For detailed architecture and API documentation, see `README.md`.
