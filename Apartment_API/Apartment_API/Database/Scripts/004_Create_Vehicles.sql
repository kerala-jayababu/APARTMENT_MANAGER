/*
  Creates dbo.Vehicles if missing (matches Apartment_API.Models.Vehicle).
  Used when saving owner/tenant flows that insert vehicle rows.

  Optional FKs: uncomment if dbo.Apartments / dbo.Persons use standard PK names.
*/
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Vehicles', N'U') IS NOT NULL
BEGIN
    PRINT 'dbo.Vehicles already exists.';
END
ELSE
BEGIN
    CREATE TABLE dbo.Vehicles
    (
        IdVehicle   INT            NOT NULL IDENTITY (1, 1)
            CONSTRAINT PK_Vehicles PRIMARY KEY,
        ApartmentId INT            NOT NULL,
        PersonId    INT            NOT NULL,
        VehicleNumber VARCHAR(20)  NOT NULL,
        Make        VARCHAR(60)    NULL,
        Color       VARCHAR(40)    NULL,
        IsActive    BIT            NOT NULL
            CONSTRAINT DF_Vehicles_IsActive DEFAULT (1),
        CreatedAt   DATETIME2(7)   NOT NULL,
        CreatedBy   INT            NOT NULL,
        UpdatedAt   DATETIME2(7)   NULL,
        UpdatedBy   INT            NULL
    );

    /*
    ALTER TABLE dbo.Vehicles WITH CHECK
      ADD CONSTRAINT FK_Vehicles_Apartments FOREIGN KEY (ApartmentId) REFERENCES dbo.Apartments (IdApartment);

    ALTER TABLE dbo.Vehicles WITH CHECK
      ADD CONSTRAINT FK_Vehicles_Persons FOREIGN KEY (PersonId) REFERENCES dbo.Persons (IdPerson);
    */

    CREATE NONCLUSTERED INDEX IX_Vehicles_Apartment_Person
        ON dbo.Vehicles (ApartmentId, PersonId)
        WHERE IsActive = 1;

    PRINT 'Created dbo.Vehicles.';
END
