# Database Schema Documentation

## Overview

The DaoPlan database uses Entity Framework Core to manage the schema. This document describes the database structure, relationships, and usage.

## Tables

### Liga (Main Entity)

**Purpose**: Represents the main dataset record containing key identifiers and metadata.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| Code | nvarchar(max) | Y | | Unique identifier code |
| Name | nvarchar(max) | Y | | Display name |
| ImportDate | datetime2 | N | | UTC timestamp of import |

**Relationships**:
- One Liga → Many FordOpls
- One Liga → Many Lonnliniers
- One Liga → Many Medarbs
- One Liga → Many MedarbJobs
- One Liga → Many ProduktOpls
- One Liga → Many Teksters
- One Liga → Many Totals
- One Liga → Many Udskrivs

**Sample Query**:
```sql
SELECT * FROM Ligas 
ORDER BY ImportDate DESC;
```

---

### FordOpl (Supporting Entity)

**Purpose**: Stores Ford Operation data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

**Sample Query**:
```sql
SELECT l.Id, l.Code, f.Data
FROM Ligas l
INNER JOIN FordOpls f ON l.Id = f.LigaId
WHERE l.Code = 'SAMPLE_CODE';
```

---

### Lonlinier (Supporting Entity)

**Purpose**: Stores Line data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### Medarb (Supporting Entity)

**Purpose**: Stores Employee data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### MedarbJob (Supporting Entity)

**Purpose**: Stores Employee Job data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### ProduktOpl (Supporting Entity)

**Purpose**: Stores Product Operation data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### Tekster (Supporting Entity)

**Purpose**: Stores Text data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### Total (Supporting Entity)

**Purpose**: Stores Total summary data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

### Udskriv (Supporting Entity)

**Purpose**: Stores Print/Output data related to a Liga.

**Columns**:
| Column Name | Data Type | Null | Key | Description |
|------------|-----------|------|-----|-------------|
| Id | int | N | PK | Primary key, auto-increment |
| LigaId | int | N | FK | Foreign key to Liga |
| Data | nvarchar(max) | Y | | JSON-serialized data |

**Foreign Key**:
- `LigaId` → `Liga.Id` (Cascade Delete)

---

## Entity Relationship Diagram (Text Format)

```
┌─────────────────────┐
│      Liga (1)       │
│─────────────────────│
│ Id (PK)             │
│ Code                │
│ Name                │
│ ImportDate          │
└──────────┬──────────┘
           │
           │ (1 : Many)
           │
    ┌──────┴──────┬──────────┬──────────┬──────────┐
    │             │          │          │          │
┌───▼───┐  ┌──────▼──┐ ┌─────▼─┐ ┌────▼────┐ ┌──▼──┐
│FordOpl│  │Lonlinier│ │Medarb │ │MedarbJob│ │...  │
└───────┘  └─────────┘ └───────┘ └─────────┘ └─────┘
```

## Query Examples

### Get Liga with all related data
```sql
SELECT 
    l.Id,
    l.Code,
    l.Name,
    l.ImportDate,
    (SELECT COUNT(*) FROM FordOpls WHERE LigaId = l.Id) AS FordOplCount,
    (SELECT COUNT(*) FROM Medarbs WHERE LigaId = l.Id) AS MedarbCount,
    (SELECT COUNT(*) FROM MedarbJobs WHERE LigaId = l.Id) AS MedarbJobCount
FROM Ligas l
ORDER BY l.ImportDate DESC;
```

### Find all records imported today
```sql
SELECT 
    'Liga' AS RecordType, COUNT(*) AS Count, GETUTCDATE() AS AsOf
FROM Ligas
WHERE CAST(ImportDate AS DATE) = CAST(GETUTCDATE() AS DATE)

UNION ALL

SELECT 
    'FordOpl', COUNT(*), GETUTCDATE()
FROM FordOpls f
WHERE CAST((SELECT ImportDate FROM Ligas WHERE Id = f.LigaId) AS DATE) = CAST(GETUTCDATE() AS DATE);
```

### Export Liga with related data to JSON-like structure
```sql
SELECT 
    l.Id,
    l.Code,
    l.Name,
    l.ImportDate,
    (
        SELECT Code, Name, ImportDate
        FROM Ligas
        WHERE Id = l.Id
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS LigaDetails,
    (
        SELECT Data
        FROM FordOpls
        WHERE LigaId = l.Id
        FOR JSON PATH
    ) AS FordOplDetails
FROM Ligas l
WHERE YEAR(l.ImportDate) = 2024;
```

### Find Liga records with most related records
```sql
SELECT TOP 10
    l.Id,
    l.Code,
    l.Name,
    COUNT(DISTINCT CASE WHEN f.Id IS NOT NULL THEN f.Id END) AS FordOplCount,
    COUNT(DISTINCT CASE WHEN m.Id IS NOT NULL THEN m.Id END) AS MedarbCount,
    COUNT(DISTINCT CASE WHEN t.Id IS NOT NULL THEN t.Id END) AS TotalCount
FROM Ligas l
LEFT JOIN FordOpls f ON l.Id = f.LigaId
LEFT JOIN Medarbs m ON l.Id = m.LigaId
LEFT JOIN Totals t ON l.Id = t.LigaId
GROUP BY l.Id, l.Code, l.Name
ORDER BY FordOplCount DESC, MedarbCount DESC;
```

## Indexing Strategy

### Recommended Indexes for Performance

```sql
-- Foreign Key Indexes (usually created automatically)
CREATE INDEX IX_FordOpl_LigaId ON FordOpls(LigaId);
CREATE INDEX IX_Lonlinier_LigaId ON Lonnliniers(LigaId);
CREATE INDEX IX_Medarb_LigaId ON Medarbs(LigaId);
CREATE INDEX IX_MedarbJob_LigaId ON MedarbJobs(LigaId);
CREATE INDEX IX_ProduktOpl_LigaId ON ProduktOpls(LigaId);
CREATE INDEX IX_Tekster_LigaId ON Teksters(LigaId);
CREATE INDEX IX_Total_LigaId ON Totals(LigaId);
CREATE INDEX IX_Udskriv_LigaId ON Udskrivs(LigaId);

-- Search Indexes
CREATE INDEX IX_Liga_Code ON Ligas(Code);
CREATE INDEX IX_Liga_ImportDate ON Ligas(ImportDate DESC);

-- Composite Indexes for common queries
CREATE INDEX IX_Liga_Code_ImportDate ON Ligas(Code, ImportDate DESC);
```

## Data Volume Estimates

Based on typical CSV imports:

| Table | Typical Rows | Storage Size |
|-------|--------------|--------------|
| Liga | 100 - 10,000 | 1 - 100 MB |
| FordOpl | 1,000 - 100,000 | 10 - 500 MB |
| Medarb | 500 - 50,000 | 5 - 250 MB |
| MedarbJob | 1,000 - 100,000 | 10 - 500 MB |
| ProduktOpl | 500 - 50,000 | 5 - 250 MB |
| Others | 100 - 10,000 | 1 - 50 MB each |
| **Total** | **5,000 - 500,000** | **50 - 2,500 MB** |

## Maintenance Tasks

### Regular Cleanup (Monthly)

```sql
-- Delete imports older than 1 year
DELETE FROM Ligas
WHERE ImportDate < DATEADD(YEAR, -1, GETUTCDATE());

-- Delete orphaned records (if any)
DELETE FROM FordOpls
WHERE LigaId NOT IN (SELECT Id FROM Ligas);
```

### Index Maintenance (Weekly)

```sql
-- Rebuild fragmented indexes
ALTER INDEX ALL ON Ligas REBUILD;
ALTER INDEX ALL ON FordOpls REBUILD;
-- ... repeat for other tables

-- Update statistics
EXEC sp_updatestats;
```

### Backup Strategy

```sql
-- Full backup
BACKUP DATABASE DaoPlanDb 
TO DISK = 'D:\Backups\DaoPlanDb_Full.bak'
WITH INIT, COMPRESSION;

-- Incremental backup (if using Full recovery model)
BACKUP LOG DaoPlanDb 
TO DISK = 'D:\Backups\DaoPlanDb_Log.bak'
WITH INIT, COMPRESSION;
```

## Monitoring Queries

### Check database size
```sql
SELECT 
    DB_NAME() AS DatabaseName,
    ROUND(SUM(size) * 8 / 1024.0, 2) AS SizeInMB
FROM sys.master_files
WHERE DB_NAME(database_id) = 'DaoPlanDb'
GROUP BY DB_NAME();
```

### Check table sizes
```sql
SELECT 
    OBJECT_NAME(ps.object_id) AS TableName,
    SUM(ps.row_count) AS RowCount,
    ROUND(SUM(ps.used_page_count) * 8 / 1024.0, 2) AS SizeInMB
FROM sys.dm_db_partition_stats ps
WHERE database_id = DB_ID('DaoPlanDb')
GROUP BY ps.object_id
ORDER BY SUM(ps.used_page_count) DESC;
```

### Check index usage
```sql
SELECT 
    OBJECT_NAME(ius.object_id) AS TableName,
    i.name AS IndexName,
    ius.user_seeks,
    ius.user_scans,
    ius.user_lookups,
    ius.user_updates
FROM sys.dm_db_index_usage_stats ius
INNER JOIN sys.indexes i ON ius.object_id = i.object_id AND ius.index_id = i.index_id
WHERE database_id = DB_ID('DaoPlanDb')
ORDER BY ius.user_seeks + ius.user_scans + ius.user_lookups DESC;
```

## Data Types Reference

| Type | Usage | Considerations |
|------|-------|-----------------|
| `int` | Identity, IDs | 32-bit integer, max ~2B |
| `nvarchar(max)` | Code, Name, JSON Data | Variable length Unicode, full-text searchable |
| `datetime2` | ImportDate | High precision (100ns), UTC storage recommended |

## Constraints

### Primary Keys
- All tables have auto-increment `Id` as primary key
- SQL Server generates values automatically

### Foreign Keys
- All supporting entities have `LigaId` foreign key
- **Delete Behavior**: Cascade (deleting Liga deletes all related records)
- **Referential Integrity**: Enforced at database level

### Not Null Constraints
- `Id`: NOT NULL (PK)
- `LigaId`: NOT NULL (FK on supporting tables)
- `ImportDate`: NOT NULL
- Other fields: NULL allowed for flexibility

## Growth Projection

**Assuming 100 new Ligas imported daily**:

| Month | Total Ligas | Total Records | Database Size |
|-------|-------------|---------------|---------------|
| Month 1 | 3,000 | 30,000 | 300 MB |
| Month 6 | 18,000 | 180,000 | 1.8 GB |
| Year 1 | 36,500 | 365,000 | 3.6 GB |
| Year 2 | 73,000 | 730,000 | 7.3 GB |

**Archiving Strategy**:
- Archive Liga records > 2 years old
- Maintain recent data for operational queries
- Use partitioning for very large tables

## Migration Guide

### If You Need to Modify Schema

1. **Add new column to Liga**:
   ```csharp
   // In Entities.cs
   public string? NewField { get; set; }
   ```

2. **Create migration**:
   ```bash
   dotnet ef migrations add AddNewFieldToLiga
   ```

3. **Update database**:
   ```bash
   dotnet ef database update
   ```

### If You Need to Add New Entity Type

1. **Create new entity class**:
   ```csharp
   public class NewEntity
   {
       public int Id { get; set; }
       public int LigaId { get; set; }
       public string Data { get; set; }
       public Liga? Liga { get; set; }
   }
   ```

2. **Add to DbContext**:
   ```csharp
   public DbSet<NewEntity> NewEntities { get; set; }
   ```

3. **Configure relationship in OnModelCreating()**:
   ```csharp
   modelBuilder.Entity<NewEntity>()
       .HasOne(x => x.Liga)
       .WithMany()
       .HasForeignKey(x => x.LigaId)
       .OnDelete(DeleteBehavior.Cascade);
   ```

4. **Create and apply migration**:
   ```bash
   dotnet ef migrations add AddNewEntity
   dotnet ef database update
   ```

---

**Last Updated**: 2024
**Database Version**: 1.0
**EF Core Version**: 8.0.0
**SQL Server Compatibility**: 2016 and later
