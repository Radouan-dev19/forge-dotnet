-- Schéma cible de la migration, absent au démarrage.
CREATE TABLE dbo.Customers (
    CustomerId int IDENTITY PRIMARY KEY,
    Name nvarchar(80) NOT NULL UNIQUE,
    IsActive bit NOT NULL
);
CREATE TABLE dbo.Orders (
    OrderId int IDENTITY PRIMARY KEY,
    CustomerId int NOT NULL REFERENCES dbo.Customers(CustomerId),
    Total decimal(10,2) NOT NULL,
    Status nvarchar(20) NOT NULL,
    CreatedAtUtc datetime2 NOT NULL,
    RowVersion rowversion NOT NULL
);
