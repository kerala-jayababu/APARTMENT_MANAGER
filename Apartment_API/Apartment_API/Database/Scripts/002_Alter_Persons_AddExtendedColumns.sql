/*
  Adds optional columns expected by Apartment_API Models.Person when missing from dbo.Persons.
  Fixes: Invalid column name Age, Gender, PermanentAddress, Relationship, SpecialNotes.

  Note: ApartmentId and LinkedUserId must already exist (standard schema); this script does not add them.

  Run after backup against the same database as DefaultConnection.
*/
SET NOCOUNT ON;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Persons') AND name = N'Age')
BEGIN
    ALTER TABLE dbo.Persons ADD Age INT NULL;
    PRINT 'Added dbo.Persons.Age.';
END
ELSE PRINT 'dbo.Persons.Age already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Persons') AND name = N'Gender')
BEGIN
    ALTER TABLE dbo.Persons ADD Gender VARCHAR(20) NULL;
    PRINT 'Added dbo.Persons.Gender.';
END
ELSE PRINT 'dbo.Persons.Gender already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Persons') AND name = N'PermanentAddress')
BEGIN
    ALTER TABLE dbo.Persons ADD PermanentAddress NVARCHAR(500) NULL;
    PRINT 'Added dbo.Persons.PermanentAddress.';
END
ELSE PRINT 'dbo.Persons.PermanentAddress already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Persons') AND name = N'Relationship')
BEGIN
    ALTER TABLE dbo.Persons ADD Relationship NVARCHAR(30) NULL;
    PRINT 'Added dbo.Persons.Relationship.';
END
ELSE PRINT 'dbo.Persons.Relationship already exists.';

IF NOT EXISTS (
    SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.Persons') AND name = N'SpecialNotes')
BEGIN
    ALTER TABLE dbo.Persons ADD SpecialNotes NVARCHAR(500) NULL;
    PRINT 'Added dbo.Persons.SpecialNotes.';
END
ELSE PRINT 'dbo.Persons.SpecialNotes already exists.';
