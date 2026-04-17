# ? EF Core Migrations - Quick Reference

## Installation Status

? **Packages Installed:**
- Microsoft.EntityFrameworkCore.Tools (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)

? **Files Created:**
- Migrations/20240101000000_InitialCreate.cs (Creates Liga table with 61 columns)
- Migrations/DaoPlanDbContextModelSnapshot.cs (Schema snapshot)
- Utilities/MigrationHelper.cs (Helper class for migrations)

? **Updated Files:**
- Program.cs (Now includes automatic migration on startup)

## Quick Commands

### Run Application (Applies Migrations)
```bash
dotnet run
```

### Check Migration Status
```bash
dotnet ef migrations list
```

### Apply All Pending Migrations
```bash
dotnet ef database update
```

### Drop Database
```bash
dotnet ef database drop
```

### Create New Migration (After modifying entity)
```bash
dotnet ef migrations add AddNewColumn
```

### Remove Last Migration
```bash
dotnet ef migrations remove
```

### Rollback to Specific Migration
```bash
dotnet ef database update [MigrationName]
```

### Rollback Everything (Remove all tables)
```bash
dotnet ef database update 0
```

## What Gets Created

### Liga Table
- **61 Columns**: Id, FileName, ImportDate, + 58 Liga data columns
- **Primary Key**: Id (auto-increment)
- **3 Indexes**:
  - IX_Liga_FileName (for file-based queries)
  - IX_Liga_ImportDate DESC (for chronological queries)
  - IX_Liga_File_Date (composite for common queries)

## Workflow

### First Time (Development)
```bash
# 1. Build project
dotnet build

# 2. Run application (creates database and applies InitialCreate migration)
dotnet run

# 3. Verify in SQL Server Management Studio
# Database: DaoPlanDb
# Table: Ligas with 61 columns
```

### Adding New Columns
```bash
# 1. Edit Entities/Entities.cs (add property to Liga class)

# 2. Create migration
dotnet ef migrations add AddNewProperty

# 3. Apply migration
dotnet ef database update

# 4. Done! New column is in database
```

### Undo Last Migration
```bash
# 1. Rollback last migration
dotnet ef migrations remove

# 2. Update database
dotnet ef database update
```

## Program.cs Integration

The application now:
1. ? Automatically initializes database
2. ? Checks for pending migrations
3. ? Applies pending migrations
4. ? Verifies Liga table
5. ? Then runs import service

```csharp
// Automatic on startup
var migrationHelper = scope.ServiceProvider.GetRequiredService<MigrationHelper>();
var dbState = await migrationHelper.InitializeDatabaseAsync();

if (dbState.PendingMigrationCount > 0)
{
    await migrationHelper.MigrateAsync();
}
```

## Useful Migration Info

### Migration File Contents

**Up()** - What gets created:
```csharp
migrationBuilder.CreateTable("Ligas", ...);
migrationBuilder.CreateIndex("IX_Liga_FileName", ...);
```

**Down()** - How to undo:
```csharp
migrationBuilder.DropTable("Ligas");
```

### Snapshot File
- Auto-generated tracking file
- Do not edit manually
- Automatically updated when running migrations

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "Pending migrations" | Run `dotnet ef database update` |
| "Cannot connect" | Check connection string in appsettings.json |
| "Table doesn't exist" | Run `dotnet run` to apply migrations |
| "Migration conflicts" | Run `dotnet ef migrations remove` and recreate |

## Build & Run

```bash
# Build
dotnet build

# Run (applies migrations automatically)
dotnet run

# Verify database created
# Check: (localdb)\mssqllocaldb database "DaoPlanDb" table "Ligas"
```

## Next Steps

1. ? **Build**: `dotnet build`
2. ? **Run**: `dotnet run` (creates database)
3. ? **Verify**: Check SQL Server for Ligas table
4. ? **Import**: Place ZIP files and run again
5. ? **Query**: Use EXAMPLES.cs for sample queries

## Documentation

For complete details, see: **MIGRATIONS_GUIDE.md**

---

**Status**: ? Ready to use
**Build**: ? Successful
**Migrations**: ? Configured
**Database**: Will be created automatically on first run
