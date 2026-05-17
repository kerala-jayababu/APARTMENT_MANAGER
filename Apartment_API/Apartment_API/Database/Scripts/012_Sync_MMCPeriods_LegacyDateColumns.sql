/*
  Syncs legacy dbo.MMCPeriods.MMCPeriodFrom / MMCPeriodTo with StartDate / EndDate when both exist.

  Run after 011_Alter_MMCPeriods_AlignModel.sql if POST still fails or legacy rows are empty.
*/
SET NOCOUNT ON;
GO

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.MMCPeriods', N'U') IS NULL
BEGIN
    PRINT 'dbo.MMCPeriods does not exist.';
    RETURN;
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'MMCPeriodFrom')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'StartDate')
BEGIN
    UPDATE p
    SET MMCPeriodFrom = COALESCE(p.MMCPeriodFrom, p.StartDate, CAST(SYSUTCDATETIME() AS DATE))
    FROM dbo.MMCPeriods p;
    PRINT 'Synced MMCPeriodFrom from StartDate where needed.';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'MMCPeriodTo')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.MMCPeriods') AND name = N'EndDate')
BEGIN
    UPDATE p
    SET MMCPeriodTo = COALESCE(
            p.MMCPeriodTo,
            p.EndDate,
            DATEADD(MONTH, 1, COALESCE(p.MMCPeriodFrom, p.StartDate, CAST(SYSUTCDATETIME() AS DATE))))
    FROM dbo.MMCPeriods p;
    PRINT 'Synced MMCPeriodTo from EndDate where needed.';
END
GO
