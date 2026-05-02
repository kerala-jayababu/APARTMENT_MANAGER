/*
  Adds FK column BlockId to dbo.Units when it does not exist (fixes EF queries using BlockId).
  Run against your society database after backup.

  Preconditions:
  - dbo.Blocks exists with PK IdBlock (matches Apartment_API Models.Block).
*/

SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Units') AND name = N'BlockId')
BEGIN
    ALTER TABLE dbo.Units ADD BlockId INT NULL;
    PRINT 'Added column dbo.Units.BlockId.';
END
ELSE
    PRINT 'Column dbo.Units.BlockId already exists.';

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Units_Blocks_BlockId')
   AND OBJECT_ID(N'dbo.Blocks', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Units WITH CHECK
    ADD CONSTRAINT FK_Units_Blocks_BlockId
        FOREIGN KEY (BlockId) REFERENCES dbo.Blocks (IdBlock);
    PRINT 'Added FK FK_Units_Blocks_BlockId.';
END
ELSE IF OBJECT_ID(N'dbo.Blocks', N'U') IS NULL
    PRINT 'Skipped FK: dbo.Blocks not found.';
ELSE
    PRINT 'FK FK_Units_Blocks_BlockId already exists or could not be added.';

/*
  Optional: backfill BlockId from legacy varchar Block name (adjust names to match your data):

  UPDATE u
  SET u.BlockId = b.IdBlock
  FROM dbo.Units u
  INNER JOIN dbo.Blocks b ON b.ApartmentId = u.ApartmentId AND b.BlockCode = u.[Block]
  WHERE u.BlockId IS NULL AND u.[Block] IS NOT NULL;
*/
