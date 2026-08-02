dbo.Orders(OrderId int PK, CreatedAtUtc datetime2, Total decimal(10,2))
INDEX IX_Orders_CreatedAtUtc_OrderId(CreatedAtUtc, OrderId) INCLUDE (Total)
