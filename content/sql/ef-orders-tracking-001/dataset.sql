CREATE TABLE dbo.Customers (CustomerId int PRIMARY KEY, Name nvarchar(80) NOT NULL UNIQUE, IsActive bit NOT NULL);
CREATE TABLE dbo.Orders (OrderId int PRIMARY KEY, CustomerId int NOT NULL REFERENCES dbo.Customers(CustomerId), Total decimal(10,2) NOT NULL, Status nvarchar(20) NOT NULL, CreatedAtUtc datetime2 NOT NULL, RowVersion rowversion NOT NULL);
CREATE INDEX IX_Orders_CustomerId_CreatedAt ON dbo.Orders(CustomerId, CreatedAtUtc);
INSERT dbo.Customers (CustomerId, Name, IsActive) VALUES (1, N'Ada', 1), (2, N'Grace', 1), (3, N'Linus', 0);
INSERT dbo.Orders (OrderId, CustomerId, Total, Status, CreatedAtUtc) VALUES
  (1, 1, 120.50, N'Paid', '2026-07-01'),
  (2, 1, 75.00, N'Pending', '2026-07-02'),
  (3, 2, 40.25, N'Paid', '2026-07-03'),
  (4, 3, 18.00, N'Cancelled', '2026-07-04');
