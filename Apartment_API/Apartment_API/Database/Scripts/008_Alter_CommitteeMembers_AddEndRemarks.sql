/*
  Adds dbo.CommitteeMembers.EndRemarks expected by Apartment_API.Models.CommitteeMember.
  Fixes: Invalid column name 'EndRemarks'.

  Run after backup against the same database as DefaultConnection (including server).
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CommitteeMembers', N'U') IS NULL
BEGIN
    RAISERROR('dbo.CommitteeMembers does not exist. Create the table before running this script.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.CommitteeMembers') AND name = N'EndRemarks')
BEGIN
    ALTER TABLE dbo.CommitteeMembers ADD EndRemarks NVARCHAR(300) NULL;
    PRINT 'Added dbo.CommitteeMembers.EndRemarks.';
END
ELSE
    PRINT 'dbo.CommitteeMembers.EndRemarks already exists.';
