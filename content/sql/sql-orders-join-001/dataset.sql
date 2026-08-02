CREATE TABLE dbo.Customers (CustomerId int PRIMARY KEY, Name nvarchar(80) NOT NULL UNIQUE, IsActive bit NOT NULL);
CREATE TABLE dbo.Orders (OrderId int PRIMARY KEY, CustomerId int NOT NULL REFERENCES dbo.Customers(CustomerId), Total decimal(10,2) NOT NULL CHECK (Total >= 0), Status nvarchar(20) NOT NULL, CreatedAtUtc datetime2 NOT NULL);
INSERT dbo.Customers VALUES (1,N'Ada',1),(2,N'Grace',1),(3,N'Linus',0);
INSERT dbo.Orders VALUES (1,1,120.50,N'Paid','2026-07-01'),(2,1,75,N'Pending','2026-07-02'),(3,2,40.25,N'Paid','2026-07-03'),(4,3,18,N'Cancelled','2026-07-04');
