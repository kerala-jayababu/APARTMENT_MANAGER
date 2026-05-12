/*
  Creates dbo.UnitStatusHistory to match Apartment_API.Models.UnitStatusHistory.

  Endpoints that depend on this table:
    - GET  /api/v1/units/status-history
    - GET  /api/v1/units/{id}/status-history
    - POST /api/v1/units/{id}/status   (inserts a row per change)

  Run against your target DB after backup.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.UnitStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UnitStatusHistory
    (
        IdUnitStatusHistory INT           NOT NULL IDENTITY(1,1)
            CONSTRAINT PK_UnitStatusHistory PRIMARY KEY,
        ApartmentId         INT           NOT NULL,
        UnitId              INT           NOT NULL,
        PreviousStatusId    INT           NOT NULL,
        NewStatusId         INT           NOT NULL,
        EffectiveDate       DATE          NOT NULL,
        LinkedPersonId      INT           NULL,
        Reason              NVARCHAR(300) NOT NULL,
        ChangedByUserId     INT           NOT NULL,
        ChangedAt           DATETIME      NOT NULL
            CONSTRAINT DF_UnitStatusHistory_ChangedAt DEFAULT (SYSUTCDATETIME())
    );
    PRINT 'Created dbo.UnitStatusHistory.';

    /* Optional FKs - only added if the parent tables exist.
       Using NO ACTION to avoid cascading deletes on history. */
    IF OBJECT_ID(N'dbo.Apartments', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.UnitStatusHistory
            ADD CONSTRAINT FK_UnitStatusHistory_Apartments
                FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments(IdApartment);
        PRINT 'Added FK_UnitStatusHistory_Apartments.';
    END

    IF OBJECT_ID(N'dbo.Units', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.UnitStatusHistory
            ADD CONSTRAINT FK_UnitStatusHistory_Units
                FOREIGN KEY (UnitId) REFERENCES dbo.Units(IdUnit);
        PRINT 'Added FK_UnitStatusHistory_Units.';
    END

    IF OBJECT_ID(N'dbo.UnitStatuses', N'U') IS NOT NULL
    BEGIN
        ALTER TABLE dbo.UnitStatusHistory
            ADD CONSTRAINT FK_UnitStatusHistory_PreviousStatus
                FOREIGN KEY (PreviousStatusId) REFERENCES dbo.UnitStatuses(IdUnitStatus);
        ALTER TABLE dbo.UnitStatusHistory
            ADD CONSTRAINT FK_UnitStatusHistory_NewStatus
                FOREIGN KEY (NewStatusId) REFERENCES dbo.UnitStatuses(IdUnitStatus);
        PRINT 'Added FK_UnitStatusHistory_(Previous|New)Status.';
    END

    CREATE INDEX IX_UnitStatusHistory_Apartment_Unit
        ON dbo.UnitStatusHistory (ApartmentId, UnitId);
    CREATE INDEX IX_UnitStatusHistory_Apartment_ChangedAt
        ON dbo.UnitStatusHistory (ApartmentId, ChangedAt DESC);
    PRINT 'Created supporting indexes on dbo.UnitStatusHistory.';
END
ELSE
BEGIN
    PRINT 'dbo.UnitStatusHistory already exists - no changes made.';
END
