CREATE TABLE dbo.Orders (OrderId int PRIMARY KEY, CustomerId int NOT NULL, Total decimal(10,2) NOT NULL, Status nvarchar(20) NOT NULL);
INSERT dbo.Orders VALUES (1,1,120.50,N'Paid'),(2,1,75,N'Pending'),(3,2,40.25,N'Paid'),(4,3,18,N'Cancelled'),(5,3,12,N'Paid');
