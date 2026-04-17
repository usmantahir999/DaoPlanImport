# EF Core Migrations Guide

## Overview

This application uses Entity Framework Core (EF Core) migrations to manage database schema changes. Migrations allow you to version control your database schema and apply changes programmatically.

## What's Installed

The following NuGet packages have been added to support migrations:

- **Microsoft.EntityFrameworkCore** (8.0.0) - Core ORM
- **Microsoft.EntityFrameworkCore.SqlServer** (8.0.0) - SQL Server provider
- **Microsoft.EntityFrameworkCore.Tools** (8.0.0) - **NEW** - CLI tools for migrations
- **Microsoft.EntityFrameworkCore.Design** (8.0.0) - **NEW** - Design-time services

## Files Created

### Migration Files

1. **Migrations/20240101000000_InitialCreate.cs**
   - Main migration file that creates the Liga table
   - Contains Up() method (creates table)
   - Contains Down() method (reverts changes)
   - Includes all 61 columns with proper data types
   - Creates 3 indexes for performance

2. **Migrations/DaoPlanDbContextModelSnapshot.cs**
   - Auto-generated snapshot of the current database model
   - Used by EF Core to track model changes
   - Do not edit manually

### Utility Class

**Utilities/MigrationHelper.cs**
- Helper methods for database operations
- Methods to apply migrations, get database state, verify tables
- Integrated into Program.cs for automatic initialization

## How Migrations Work

### Migration Process

```
Model Changes (Entities.cs)
        ?
Create Migration (generates .cs file)
        ?
Build Project
        ?
Apply Migration to Database (creates/alters tables)
        ?
Database Updated
```

### Current State

The **InitialCreate** migration will:
- Create the `Ligas` table with 61 columns
- Create identity for the `Id` column
- Create 3 indexes:
  - `IX_Liga_FileName` - For file-based queries
  - `IX_Liga_ImportDate` - For chronological queries (descending)
  - `IX_Liga_File_Date` - Composite index for common queries

## Running Migrations

### Automatic (Programmatically)

The application now automatically runs migrations on startup:

```csharp
// In Program.cs
var migrationHelper = scope.ServiceProvider.GetRequiredService<MigrationHelper>();
var dbState = await migrationHelper.InitializeDatabaseAsync();

if (dbState.PendingMigrationCount > 0)
{
    await migrationHelper.MigrateAsync();
}
```

**To run:**
```bash
dotnet run
```

### Manual (CLI Commands)

Install EF Core CLI tool (if not already installed):
```bash
dotnet tool install --global dotnet-ef
```

#### Apply Migrations
```bash
# Apply all pending migrations
dotnet ef database update

# Apply up to a specific migration
dotnet ef database update 20240101000000_InitialCreate

# Check migration status
dotnet ef migrations list
```

#### Check Migration Status
```bash
# List all migrations
dotnet ef migrations list

# List with details
dotnet ef migrations list --verbose
```

#### Rollback/Downgrade
```bash
# Rollback to initial state (removes all tables)
dotnet ef database update 0

# Rollback to a specific migration
dotnet ef database update [previous-migration-name]
```

#### Drop Database
```bash
# Drops the database (requires confirmation)
dotnet ef database drop

# Drop without confirmation
dotnet ef database drop --force
```

## Creating New Migrations

When you modify the entity model (e.g., add/remove columns), create a migration:

```bash
# Create a new migration
dotnet ef migrations add [MigrationName]

# Example
dotnet ef migrations add AddNewColumnToLiga
```

This will:
1. Generate a new migration file (timestamped)
2. Analyze changes from snapshot
3. Create Up() and Down() methods

Then apply it:
```bash
dotnet ef database update
```

## Migration File Structure

### Up Method (Apply Changes)
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "Ligas",
        columns: table => new
        {
            Id = table.Column<int>(nullable: false)
                .Annotation("SqlServer:Identity", "1, 1"),
            // ... more columns
        },
        constraints: table =>
        {
            table.PrimaryKey("PK_Ligas", x => x.Id);
        });

    migrationBuilder.CreateIndex(
        name: "IX_Liga_FileName",
        table: "Ligas",
        column: "FileName");
}
```

### Down Method (Rollback Changes)
```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropTable(
        name: "Ligas");
}
```

## Migration Helper Methods

The `MigrationHelper` class provides convenient methods:

### Initialize Database
```csharp
var dbState = await migrationHelper.InitializeDatabaseAsync();
Console.WriteLine($"Applied: {dbState.AppliedMigrationCount}");
Console.WriteLine($"Pending: {dbState.PendingMigrationCount}");
```

### Apply Pending Migrations
```csharp
await migrationHelper.MigrateAsync();
```

### Check Database State
```csharp
var state = await migrationHelper.GetDatabaseStateAsync();
if (state.IsConnected)
{
    Console.WriteLine(state.AppliedMigrations);
}
```

### Verify Liga Table
```csharp
bool verified = await migrationHelper.VerifyLigaTableAsync();
if (verified)
{
    Console.WriteLine("Liga table exists and is accessible");
}
```

## Database State Tracking

The `DatabaseState` class provides information:

```csharp
public class DatabaseState
{
    public bool IsConnected { get; set; }
    public int AppliedMigrationCount { get; set; }
    public int PendingMigrationCount { get; set; }
    public List<string> AppliedMigrations { get; set; }
    public List<string> PendingMigrations { get; set; }
    public string? Error { get; set; }
}
```

## Common Scenarios

### Scenario 1: First Time Setup

```bash
# 1. Build project
dotnet build

# 2. Run application (applies InitialCreate migration)
dotnet run

# 3. Verify database and table created
```

### Scenario 2: Add New Column to Liga

```bash
# 1. Add property to Liga entity class
public string NewColumn { get; set; }

# 2. Create migration
dotnet ef migrations add AddNewColumnToLiga

# 3. Review generated migration file (Migrations/[timestamp]_AddNewColumnToLiga.cs)

# 4. Apply migration
dotnet ef database update

# 5. Application can now use the new column
```

### Scenario 3: Rollback Last Migration

```bash
# 1. Find previous migration name
dotnet ef migrations list

# 2. Rollback to previous migration
dotnet ef database update [previous-migration-name]

# 3. Remove migration file (optional)
dotnet ef migrations remove
```

### Scenario 4: Clean Database and Reapply

```bash
# 1. Remove all migrations (reset to initial state)
dotnet ef database update 0

# 2. Reapply all migrations
dotnet ef database update
```

## Connection String Configuration

The connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DaoPlanDb;Trusted_Connection=true;"
  }
}
```

### SQL Server Options

**LocalDB (Development)**
```
Server=(localdb)\mssqllocaldb;Database=DaoPlanDb;Trusted_Connection=true;
```

**SQL Server Express**
```
Server=.\SQLEXPRESS;Database=DaoPlanDb;Trusted_Connection=true;
```

**Named Instance**
```
Server=ServerName\InstanceName;Database=DaoPlanDb;Trusted_Connection=true;
```

**SQL Authentication**
```
Server=ServerAddress;Database=DaoPlanDb;User Id=sa;Password=YourPassword;
```

**Azure SQL Database**
```
Server=tcp:servername.database.windows.net,1433;Initial Catalog=DaoPlanDb;Persist Security Info=False;User ID=username;Password=password;
```

## Troubleshooting

### Problem: "No migrations were applied"

**Solution**: Check if migrations are pending
```bash
dotnet ef migrations list
dotnet ef database update
```

### Problem: "Cannot connect to database"

**Solution**: Verify connection string and SQL Server is running
```bash
# Test connection with SQL Server Management Studio
# Or verify LocalDB is running: sqllocaldb start mssqllocaldb
```

### Problem: "Schema mismatch between model and database"

**Solution**: Create a new migration to sync
```bash
dotnet ef migrations add SyncSchemaChanges
dotnet ef database update
```

### Problem: "Migrations folder missing"

**Solution**: EF Core creates it automatically, or create manually:
```bash
mkdir Migrations
dotnet ef migrations add InitialCreate
```

### Problem: Entity not found

**Solution**: Ensure using directives are correct:
```csharp
using DaoPlanImport.Data;      // For DbContext
using DaoPlanImport.Entities;  // For entities
```

## Migration Naming Conventions

| Change | Migration Name | Example |
|--------|---------------|---------|
| Initial | InitialCreate | 20240101000000_InitialCreate |
| Add Column | Add[ColumnName]To[TableName] | 20240102000000_AddNewColumnToLiga |
| Remove Column | Remove[ColumnName]From[TableName] | 20240102000000_RemoveUnusedColumnFromLiga |
| Add Index | Add[IndexName]Index | 20240102000000_AddPostalCodeIndex |
| Create Table | Create[TableName]Table | 20240102000000_CreateAuditTable |

## Best Practices

### ? DO

- Create migrations for every schema change
- Review generated migrations before applying
- Test migrations on development database first
- Keep migrations small and focused
- Use descriptive migration names
- Document complex migrations

### ? DON'T

- Edit generated migration files manually (unless you know what you're doing)
- Skip migrations in production
- Create migrations without building first
- Apply migrations to production without testing
- Delete migration files without proper cleanup
- Share uncommitted migrations between developers

## Version Control

### Git Workflow

```bash
# 1. Create migration locally
dotnet ef migrations add NewFeature

# 2. Commit migration files
git add Migrations/
git add Entities/Entities.cs
git commit -m "Add NewFeature migration"

# 3. Push to repository
git push

# 4. Other developers pull and apply
git pull
dotnet ef database update
```

### Migration Files to Track

- ? `Migrations/*.cs` - Version control all migration files
- ? `Migrations/DaoPlanDbContextModelSnapshot.cs` - Track snapshot
- ? `Migrations/DaoPlanDbContextModelSnapshot.cs` (if auto-generated by mistake)

## Monitoring and Logging

The application logs migration details:

```
Information: Initializing database with migrations
Information: Database State: Connected - Applied: 1, Pending: 0
Information: Liga table verified. Current record count: 0
Information: Database initialized successfully
```

Enable debug logging to see more details:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Information"
    }
  }
}
```

## Performance Considerations

### Index Strategy

The Liga table has 3 strategic indexes:

```sql
-- 1. File-based queries
CREATE INDEX IX_Liga_FileName ON Ligas(FileName);

-- 2. Temporal queries
CREATE INDEX IX_Liga_ImportDate ON Ligas(ImportDate DESC);

-- 3. Common combined queries
CREATE INDEX IX_Liga_File_Date ON Ligas(FileName, ImportDate DESC);
```

### Adding More Indexes

To add indexes, create a new migration:

```csharp
// In a new migration file
migrationBuilder.CreateIndex(
    name: "IX_Liga_PostalCode",
    table: "Ligas",
    column: "POSTNR");
```

Then apply:
```bash
dotnet ef migrations add AddPostalCodeIndex
dotnet ef database update
```

## Summary

- ? Migrations installed and configured
- ? InitialCreate migration ready
- ? MigrationHelper utility created
- ? Automatic migration on startup
- ? CLI tools available for manual control
- ? Full tracking and rollback capability

**Next Steps:**
1. Run `dotnet run` to apply migrations
2. Verify Liga table in SQL Server
3. Start importing data with CSV files
4. Create new migrations as schema evolves

---

**Status**: ? COMPLETE & READY FOR USE
