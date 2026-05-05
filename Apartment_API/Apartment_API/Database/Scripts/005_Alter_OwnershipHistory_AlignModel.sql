/*
  Aligns dbo.OwnershipHistory with Apartment_API.Models.OwnershipHistory.
  Fixes missing columns like:
    IdOwnershipHistory, PreviousOwnerPersonId, NewOwnerPersonId, SaleDeedReference,
    TransferValue, DeedDocumentId, Remarks, RecordedByUserId, RecordedAt.

  Run against your target DB after backup.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.OwnershipHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OwnershipHistory
    (
        IdOwnershipHistory    INT           NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_OwnershipHistory PRIMARY KEY,
        ApartmentId           INT           NOT NULL,
        UnitId                INT           NOT NULL,
        PreviousOwnerPersonId INT           NULL,
        NewOwnerPersonId      INT           NOT NULL,
        TransferType          VARCHAR(20)   NOT NULL CONSTRAINT DF_OwnershipHistory_TransferType DEFAULT ('Sale'),
        TransferDate          DATE          NOT NULL,
        SaleDeedReference     VARCHAR(100)  NULL,
        TransferValue         DECIMAL(14,2) NULL,
        DeedDocumentId        INT           NULL,
        Remarks               NVARCHAR(500) NULL,
        RecordedByUserId      INT           NOT NULL,
        RecordedAt            DATETIME2(7)  NOT NULL
    );
    PRINT 'Created dbo.OwnershipHistory.';
END
ELSE
BEGIN
    DECLARE @ExistingIdentityColumn SYSNAME;
    SELECT TOP (1) @ExistingIdentityColumn = c.name
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID(N'dbo.OwnershipHistory')
      AND c.is_identity = 1
    ORDER BY c.column_id;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'IdOwnershipHistory')
    BEGIN
        IF @ExistingIdentityColumn IS NULL
        BEGIN
            ALTER TABLE dbo.OwnershipHistory ADD IdOwnershipHistory INT IDENTITY(1,1) NOT NULL;
            PRINT 'Added dbo.OwnershipHistory.IdOwnershipHistory as identity.';
        END
        ELSE
        BEGIN
            DECLARE @RenameSql NVARCHAR(400);
            SET @RenameSql = N'EXEC sp_rename ''dbo.OwnershipHistory.' + REPLACE(@ExistingIdentityColumn, '''', '''''') + ''', ''IdOwnershipHistory'', ''COLUMN'';';
            EXEC sp_executesql @RenameSql;
            PRINT 'Renamed existing identity column to dbo.OwnershipHistory.IdOwnershipHistory.';
        END
    END
    ELSE PRINT 'dbo.OwnershipHistory.IdOwnershipHistory already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'ApartmentId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD ApartmentId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.ApartmentId as NULL (backfill then alter if needed).';
    END
    ELSE PRINT 'dbo.OwnershipHistory.ApartmentId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'UnitId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD UnitId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.UnitId as NULL (backfill then alter if needed).';
    END
    ELSE PRINT 'dbo.OwnershipHistory.UnitId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'PreviousOwnerPersonId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD PreviousOwnerPersonId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.PreviousOwnerPersonId.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.PreviousOwnerPersonId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'NewOwnerPersonId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD NewOwnerPersonId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.NewOwnerPersonId as NULL (backfill then alter if needed).';
    END
    ELSE PRINT 'dbo.OwnershipHistory.NewOwnerPersonId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'TransferType')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD TransferType VARCHAR(20) NULL;
        PRINT 'Added dbo.OwnershipHistory.TransferType.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.TransferType already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'TransferDate')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD TransferDate DATE NULL;
        PRINT 'Added dbo.OwnershipHistory.TransferDate.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.TransferDate already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'SaleDeedReference')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD SaleDeedReference VARCHAR(100) NULL;
        PRINT 'Added dbo.OwnershipHistory.SaleDeedReference.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.SaleDeedReference already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'TransferValue')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD TransferValue DECIMAL(14,2) NULL;
        PRINT 'Added dbo.OwnershipHistory.TransferValue.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.TransferValue already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'DeedDocumentId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD DeedDocumentId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.DeedDocumentId.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.DeedDocumentId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'Remarks')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD Remarks NVARCHAR(500) NULL;
        PRINT 'Added dbo.OwnershipHistory.Remarks.';
    END
    ELSE PRINT 'dbo.OwnershipHistory.Remarks already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'RecordedByUserId')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD RecordedByUserId INT NULL;
        PRINT 'Added dbo.OwnershipHistory.RecordedByUserId as NULL (backfill then alter if needed).';
    END
    ELSE PRINT 'dbo.OwnershipHistory.RecordedByUserId already exists.';

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'RecordedAt')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory ADD RecordedAt DATETIME2(7) NULL;
        PRINT 'Added dbo.OwnershipHistory.RecordedAt as NULL (backfill then alter if needed).';
    END
    ELSE PRINT 'dbo.OwnershipHistory.RecordedAt already exists.';

    IF NOT EXISTS (
        SELECT 1
        FROM sys.key_constraints kc
        WHERE kc.parent_object_id = OBJECT_ID(N'dbo.OwnershipHistory')
          AND kc.[type] = 'PK')
       AND EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.OwnershipHistory') AND name = N'IdOwnershipHistory')
    BEGIN
        ALTER TABLE dbo.OwnershipHistory
            ADD CONSTRAINT PK_OwnershipHistory PRIMARY KEY (IdOwnershipHistory);
        PRINT 'Added PK_OwnershipHistory on IdOwnershipHistory.';
    END
    ELSE
        PRINT 'Primary key already exists (or IdOwnershipHistory missing).';
END

