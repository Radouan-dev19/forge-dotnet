dbo.Customers(CustomerId int PK, Name nvarchar(80) UNIQUE, IsActive bit)
dbo.Orders(OrderId int PK, CustomerId int FK -> Customers, Total decimal(10,2) CHECK >= 0, Status nvarchar(20), CreatedAtUtc datetime2)
