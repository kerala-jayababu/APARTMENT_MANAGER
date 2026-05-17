/*
  Creates dbo.CommitteeTenureExtensionLog to match Apartment_API.Models.CommitteeTenureExtensionLog.

  Endpoints that depend on this table:
    - GET  /api/v1/committee-tenures/{id}/extensions
    - POST /api/v1/committee-tenures/{id}/extend  (or equivalent extend action)

  Run against your target DB after backup.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.CommitteeTenureExtensionLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CommitteeTenureExtensionLog
    (
        IdCommitteeTenureExtensionLog INT            NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_CommitteeTenureExtensionLog PRIMARY KEY,
        ApartmentId                   INT            NOT NULL,
        CommitteeTenureId             INT            NOT NULL,
        PreviousEndDate               DATE           NOT NULL,
        NewEndDate                    DATE           NOT NULL,
        ExtensionReason               NVARCHAR(500)  NOT NULL,
        ExtendedByUserId              INT            NOT NULL,
        ExtendedAt                    DATETIME       NOT NULL
            CONSTRAINT DF_CommitteeTenureExtensionLog_ExtendedAt DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.CommitteeTenureExtensionLog.';

    IF OBJECT_ID(N'dbo.Apartments', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.CommitteeTenureExtensionLog
            ADD CONSTRAINT FK_CommitteeTenureExtensionLog_Apartments
                FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(IdApartment);
        PRINT 'Added FK_CommitteeTenureExtensionLog_Apartments.';
    END

    IF OBJECT_ID(N'dbo.CommitteeTenures', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.CommitteeTenureExtensionLog
            ADD CONSTRAINT FK_CommitteeTenureExtensionLog_CommitteeTenures
                FOREIGN KEY (CommitteeTenureId) REFERENCES dbo.CommitteeTenures(IdCommitteeTenure);
        PRINT 'Added FK_CommitteeTenureExtensionLog_CommitteeTenures.';
    END

    IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.CommitteeTenureExtensionLog
            ADD CONSTRAINT FK_CommitteeTenureExtensionLog_Users
                FOREIGN KEY (ExtendedByUserId) REFERENCES dbo.Users(IdUser);
        PRINT 'Added FK_CommitteeTenureExtensionLog_Users.';
    END

    CREATE INDEX IX_CommitteeTenureExtensionLog_Apartment_Tenure
        ON dbo.CommitteeTenureExtensionLog (ApartmentId, CommitteeTenureId);

    CREATE INDEX IX_CommitteeTenureExtensionLog_Tenure_ExtendedAt
        ON dbo.CommitteeTenureExtensionLog (CommitteeTenureId, ExtendedAt DESC);

    PRINT 'Created supporting indexes on dbo.CommitteeTenureExtensionLog.';
END
ELSE
BEGIN
    PRINT 'dbo.CommitteeTenureExtensionLog already exists - no changes made.';
END
