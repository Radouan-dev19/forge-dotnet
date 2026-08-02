CREATE TABLE dbo.Orders (OrderId int PRIMARY KEY, Total decimal(10,2) NOT NULL, Status nvarchar(20) NOT NULL, CreatedAtUtc datetime2 NOT NULL);
INSERT dbo.Orders VALUES (1,80,N'Paid','2026-06-15'),(2,30,N'Cancelled','2026-06-20'),(3,120.50,N'Paid','2026-07-01'),(4,75,N'Pending','2026-07-02'),(5,40.25,N'Paid','2026-07-03'),(6,10,N'Paid','2026-07-20');
