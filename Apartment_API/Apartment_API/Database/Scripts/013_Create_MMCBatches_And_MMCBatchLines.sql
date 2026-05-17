/*
  Creates dbo.MMCBatches and dbo.MMCBatchLines to match Apartment_API.Models.MmcBatch / MmcBatchLine.

  Endpoints that depend on these tables:
    - GET  /api/v1/mmc/{mmcPeriodId}/units
    - POST /api/v1/mmc/batches
    - GET  /api/v1/mmc/batches
    - MMC approve/reject flows

  Run against your target DB after backup.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCBatches', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MMCBatches
    (
        IdMMCBatch         INT            NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_MMCBatches PRIMARY KEY,
        ApartmentId        INT            NOT NULL,
        MmcPeriodId        INT            NOT NULL,
        StatusCode         VARCHAR(20)    NOT NULL
            CONSTRAINT DF_MMCBatches_StatusCode DEFAULT ('PENDING'),
        Remarks            NVARCHAR(500)  NULL,
        SubmittedByUserId  INT            NOT NULL,
        SubmittedAt        DATETIME       NOT NULL
            CONSTRAINT DF_MMCBatches_SubmittedAt DEFAULT (SYSUTCDATETIME()),
        ApprovedByUserId   INT            NULL,
        ApprovedAt         DATETIME       NULL,
        RejectedByUserId   INT            NULL,
        RejectedAt         DATETIME       NULL,
        RejectionReason    NVARCHAR(500)  NULL,
        IsActive           BIT            NOT NULL
            CONSTRAINT DF_MMCBatches_IsActive DEFAULT (1),
        CreatedAt          DATETIME       NOT NULL
            CONSTRAINT DF_MMCBatches_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy          INT            NOT NULL,
        UpdatedAt          DATETIME       NULL,
        UpdatedBy          INT            NULL
    );
    PRINT 'Created dbo.MMCBatches.';
END
ELSE
    PRINT 'dbo.MMCBatches already exists.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCBatchLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MMCBatchLines
    (
        IdMMCBatchLine     INT            NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_MMCBatchLines PRIMARY KEY,
        MmcBatchId         INT            NOT NULL,
        UnitId             INT            NOT NULL,
        PreviousMmcAmount  DECIMAL(14, 2) NOT NULL
            CONSTRAINT DF_MMCBatchLines_PreviousMmcAmount DEFAULT (0),
        NewMmcAmount       DECIMAL(14, 2) NOT NULL
            CONSTRAINT DF_MMCBatchLines_NewMmcAmount DEFAULT (0)
    );
    PRINT 'Created dbo.MMCBatchLines.';
END
ELSE
    PRINT 'dbo.MMCBatchLines already exists.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCBatches', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Apartments', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MMCBatches_Apartments')
BEGIN
    ALTER TABLE dbo.MMCBatches
        ADD CONSTRAINT FK_MMCBatches_Apartments
            FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments (IdApartment);
    PRINT 'Added FK_MMCBatches_Apartments.';
END

IF OBJECT_ID(N'dbo.MMCBatches', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MMCBatches_MMCPeriods')
BEGIN
    ALTER TABLE dbo.MMCBatches
        ADD CONSTRAINT FK_MMCBatches_MMCPeriods
            FOREIGN KEY (MmcPeriodId) REFERENCES dbo.MMCPeriods (IdMMCPeriod);
    PRINT 'Added FK_MMCBatches_MMCPeriods.';
END

IF OBJECT_ID(N'dbo.MMCBatchLines', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.MMCBatches', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MMCBatchLines_MMCBatches')
BEGIN
    ALTER TABLE dbo.MMCBatchLines
        ADD CONSTRAINT FK_MMCBatchLines_MMCBatches
            FOREIGN KEY (MmcBatchId) REFERENCES dbo.MMCBatches (IdMMCBatch);
    PRINT 'Added FK_MMCBatchLines_MMCBatches.';
END

IF OBJECT_ID(N'dbo.MMCBatchLines', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MMCBatchLines_Units')
BEGIN
    ALTER TABLE dbo.MMCBatchLines
        ADD CONSTRAINT FK_MMCBatchLines_Units
            FOREIGN KEY (UnitId) REFERENCES dbo.Units (IdUnit);
    PRINT 'Added FK_MMCBatchLines_Units.';
END

IF OBJECT_ID(N'dbo.MMCBatches', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_MMCBatches_Apartment_Period_Status'
         AND object_id = OBJECT_ID(N'dbo.MMCBatches'))
BEGIN
    CREATE INDEX IX_MMCBatches_Apartment_Period_Status
        ON dbo.MMCBatches (ApartmentId, MmcPeriodId, StatusCode)
        INCLUDE (SubmittedAt, IsActive);
    PRINT 'Created IX_MMCBatches_Apartment_Period_Status.';
END

IF OBJECT_ID(N'dbo.MMCBatchLines', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_MMCBatchLines_Batch_Unit'
         AND object_id = OBJECT_ID(N'dbo.MMCBatchLines'))
BEGIN
    CREATE UNIQUE INDEX IX_MMCBatchLines_Batch_Unit
        ON dbo.MMCBatchLines (MmcBatchId, UnitId);
    PRINT 'Created IX_MMCBatchLines_Batch_Unit.';
END
GO
