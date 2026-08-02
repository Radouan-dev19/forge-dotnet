dbo.Orders(OrderId int PK, CustomerId int, Total decimal(10,2), CreatedAtUtc datetime2)
INDEX IX_Orders_CustomerId_CreatedAt(CustomerId, CreatedAtUtc) INCLUDE (Total)
