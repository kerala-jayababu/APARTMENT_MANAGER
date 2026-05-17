/*
  Adds MainHeadId to dbo.ExpenseHeads when missing (matches Apartment_API.Models.ExpenseHead).

  Used by GET /api/v1/ExpenseHeads, budget flows, and expense head grouping under ExpenseMainHeads.

  Run against your target DB after backup.

  Note: GO separates batches so CREATE INDEX compiles after the column exists.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ExpenseHeads', N'U') IS NULL
BEGIN
    RAISERROR('dbo.ExpenseHeads does not exist. Create the table before running this script.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.ExpenseHeads') AND name = N'MainHeadId')
BEGIN
    ALTER TABLE dbo.ExpenseHeads ADD MainHeadId INT NULL;
    PRINT 'Added dbo.ExpenseHeads.MainHeadId.';
END
ELSE
    PRINT 'dbo.ExpenseHeads.MainHeadId already exists.';
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ExpenseHeads', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ExpenseMainHeads', N'U') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM sys.foreign_keys
       WHERE name = N'FK_ExpenseHeads_ExpenseMainHeads_MainHeadId')
BEGIN
    ALTER TABLE dbo.ExpenseHeads
        ADD CONSTRAINT FK_ExpenseHeads_ExpenseMainHeads_MainHeadId
            FOREIGN KEY (MainHeadId) REFERENCES dbo.ExpenseMainHeads (IdExpenseMainHead);
    PRINT 'Added FK_ExpenseHeads_ExpenseMainHeads_MainHeadId.';
END
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ExpenseHeads', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1 FROM sys.columns
       WHERE object_id = OBJECT_ID(N'dbo.ExpenseHeads') AND name = N'MainHeadId')
   AND NOT EXISTS (
       SELECT 1 FROM sys.indexes
       WHERE name = N'IX_ExpenseHeads_MainHeadId'
         AND object_id = OBJECT_ID(N'dbo.ExpenseHeads'))
BEGIN
    CREATE INDEX IX_ExpenseHeads_MainHeadId ON dbo.ExpenseHeads (MainHeadId)
        WHERE MainHeadId IS NOT NULL;
    PRINT 'Created IX_ExpenseHeads_MainHeadId.';
END
ELSE IF OBJECT_ID(N'dbo.ExpenseHeads', N'U') IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.ExpenseHeads') AND name = N'MainHeadId')
    PRINT 'Skipped index: MainHeadId column is missing on dbo.ExpenseHeads.';
GO
