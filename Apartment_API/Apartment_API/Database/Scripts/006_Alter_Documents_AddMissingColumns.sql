/*
  Aligns dbo.Documents with Apartment_API.Models.StoredDocument for deed upload flow.
  Adds missing nullable columns if they don't exist:
    - Description (nvarchar(300))
    - UpdatedAt (datetime2(7))
    - UpdatedBy (int)
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    PRINT 'dbo.Documents table not found. Create base table first.';
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Documents') AND name = N'Description')
BEGIN
    ALTER TABLE dbo.Documents ADD Description NVARCHAR(300) NULL;
    PRINT 'Added dbo.Documents.Description.';
END
ELSE
    PRINT 'dbo.Documents.Description already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Documents') AND name = N'UpdatedAt')
BEGIN
    ALTER TABLE dbo.Documents ADD UpdatedAt DATETIME2(7) NULL;
    PRINT 'Added dbo.Documents.UpdatedAt.';
END
ELSE
    PRINT 'dbo.Documents.UpdatedAt already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Documents') AND name = N'UpdatedBy')
BEGIN
    ALTER TABLE dbo.Documents ADD UpdatedBy INT NULL;
    PRINT 'Added dbo.Documents.UpdatedBy.';
END
ELSE
    PRINT 'dbo.Documents.UpdatedBy already exists.';

