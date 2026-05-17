/*
  Adds InvoiceSource to dbo.Invoices when missing (matches Apartment_API.Models.Invoice).

  Required for amenity booking flows that insert invoices with InvoiceSource = 'AMENITY_BOOKING'.

  Run against your target DB after backup.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    RAISERROR('dbo.Invoices does not exist. Create the table before running this script.', 16, 1);
    RETURN;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.Invoices') AND name = N'InvoiceSource')
BEGIN
    ALTER TABLE dbo.Invoices ADD InvoiceSource NVARCHAR(30) NULL;
    PRINT 'Added dbo.Invoices.InvoiceSource.';
END
ELSE
    PRINT 'dbo.Invoices.InvoiceSource already exists.';
GO
