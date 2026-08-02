dbo.Customers(CustomerId int PK, Name nvarchar(80) UNIQUE, IsActive bit)
dbo.Orders(OrderId int PK, CustomerId int FK, Total decimal(10,2), Status nvarchar(20), CreatedAtUtc datetime2, RowVersion rowversion concurrency token)
