/*
  Adds OwnershipSharePct to dbo.UnitOwners when missing (matches Apartment_API.Models.UnitOwner).

  EF expects DECIMAL(5,2) nullable — co-owner share percentage.

  Run against your society DB after backup.
*/
SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.UnitOwners') AND name = N'OwnershipSharePct')
BEGIN
    ALTER TABLE dbo.UnitOwners ADD OwnershipSharePct DECIMAL(5, 2) NULL;
    PRINT 'Added dbo.UnitOwners.OwnershipSharePct.';
END
ELSE
    PRINT 'dbo.UnitOwners.OwnershipSharePct already exists.';
