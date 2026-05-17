/*
  Aligns dbo.MMCPeriods with Apartment_API.Models.MmcPeriod.

  Endpoints that depend on this table:
    - GET  /api/v1/mmc-periods
    - GET  /api/v1/mmc-periods/current
    - POST /api/v1/mmc-periods

  Run against your target DB after backup.

  Uses GO between batches so UPDATE/INDEX compile after columns exist.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MMCPeriods
    (
        IdMMCPeriod  INT           NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_MMCPeriods PRIMARY KEY,
        ApartmentId  INT           NOT NULL,
        PeriodCode   NVARCHAR(20)  NOT NULL,
        PeriodName   NVARCHAR(60)  NOT NULL,
        StartDate    DATE          NOT NULL,
        EndDate      DATE          NOT NULL,
        IsCurrent    BIT           NOT NULL
            CONSTRAINT DF_MMCPeriods_IsCurrent DEFAULT (0),
        IsActive     BIT           NOT NULL
            CONSTRAINT DF_MMCPeriods_IsActive DEFAULT (1),
        CreatedAt    DATETIME      NOT NULL
            CONSTRAINT DF_MMCPeriods_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy    INT           NOT NULL,
        UpdatedAt    DATETIME      NULL,
        UpdatedBy    INT           NULL
    );
    PRINT 'Created dbo.MMCPeriods.';
END
ELSE
    PRINT 'dbo.MMCPeriods already exists.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NULL
BEGIN
    RAISERROR('dbo.MMCPeriods does not exist. Create-table batch failed.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'ApartmentId')
    ALTER TABLE dbo.MMCPeriods ADD ApartmentId INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'PeriodCode')
    ALTER TABLE dbo.MMCPeriods ADD PeriodCode NVARCHAR(20) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'PeriodName')
    ALTER TABLE dbo.MMCPeriods ADD PeriodName NVARCHAR(60) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'StartDate')
    ALTER TABLE dbo.MMCPeriods ADD StartDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'EndDate')
    ALTER TABLE dbo.MMCPeriods ADD EndDate DATE NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'IsCurrent')
    ALTER TABLE dbo.MMCPeriods ADD IsCurrent BIT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'IsActive')
    ALTER TABLE dbo.MMCPeriods ADD IsActive BIT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'CreatedAt')
    ALTER TABLE dbo.MMCPeriods ADD CreatedAt DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'CreatedBy')
    ALTER TABLE dbo.MMCPeriods ADD CreatedBy INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'UpdatedAt')
    ALTER TABLE dbo.MMCPeriods ADD UpdatedAt DATETIME NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'UpdatedBy')
    ALTER TABLE dbo.MMCPeriods ADD UpdatedBy INT NULL;

PRINT 'Column-add batch completed for dbo.MMCPeriods.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'PeriodCode')
BEGIN
    UPDATE p
    SET
        PeriodCode = COALESCE(NULLIF(LTRIM(RTRIM(p.PeriodCode)), N''), N'P' + RIGHT(N'0000' + CAST(p.IdMMCPeriod AS NVARCHAR(10)), 4)),
        PeriodName = COALESCE(NULLIF(LTRIM(RTRIM(p.PeriodName)), N''), N'Period ' + CAST(p.IdMMCPeriod AS NVARCHAR(10))),
        StartDate = COALESCE(p.StartDate, CAST(SYSUTCDATETIME() AS DATE)),
        EndDate = COALESCE(p.EndDate, DATEADD(MONTH, 1, COALESCE(p.StartDate, CAST(SYSUTCDATETIME() AS DATE)))),
        IsCurrent = COALESCE(p.IsCurrent, 0),
        IsActive = COALESCE(p.IsActive, 1),
        CreatedAt = COALESCE(p.CreatedAt, SYSUTCDATETIME()),
        CreatedBy = COALESCE(p.CreatedBy, 1),
        ApartmentId = COALESCE(p.ApartmentId, 1)
    FROM dbo.MMCPeriods p;

    PRINT 'Backfilled NULL values on dbo.MMCPeriods.';
END
ELSE
    PRINT 'Skipped backfill: dbo.MMCPeriods or PeriodCode column missing.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'PeriodCode' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN PeriodCode NVARCHAR(20) NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'PeriodName' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN PeriodName NVARCHAR(60) NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'StartDate' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN StartDate DATE NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'EndDate' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN EndDate DATE NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'IsCurrent' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN IsCurrent BIT NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'IsActive' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN IsActive BIT NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'CreatedAt' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN CreatedAt DATETIME NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'CreatedBy' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN CreatedBy INT NOT NULL;

    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'ApartmentId' AND is_nullable = 1)
        ALTER TABLE dbo.MMCPeriods ALTER COLUMN ApartmentId INT NOT NULL;

    PRINT 'Enforced NOT NULL on dbo.MMCPeriods core columns.';
END
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Apartments', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MMCPeriods_Apartments')
BEGIN
    ALTER TABLE dbo.MMCPeriods
        ADD CONSTRAINT FK_MMCPeriods_Apartments
            FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments (IdApartment);
    PRINT 'Added FK_MMCPeriods_Apartments.';
END

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NOT NULL
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'StartDate')
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_MMCPeriods_Apartment_StartDate'
         AND object_id = OBJECT_ID(N'dbo.MMCPeriods'))
BEGIN
    CREATE INDEX IX_MMCPeriods_Apartment_StartDate
        ON dbo.MMCPeriods (ApartmentId, StartDate DESC);
    PRINT 'Created IX_MMCPeriods_Apartment_StartDate.';
END
GO
