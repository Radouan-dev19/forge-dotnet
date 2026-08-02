DELETE FROM dbo.Orders;
DELETE FROM dbo.Customers;
INSERT dbo.Customers (CustomerId, Name, IsActive) VALUES (1, N'Ada', 1), (2, N'Grace', 1), (3, N'Linus', 0);
INSERT dbo.Orders (OrderId, CustomerId, Total, Status, CreatedAtUtc) VALUES (1,1,120.50,N'Paid','2026-07-01'),(2,1,75,N'Pending','2026-07-02'),(3,2,40.25,N'Paid','2026-07-03'),(4,3,18,N'Cancelled','2026-07-04');
