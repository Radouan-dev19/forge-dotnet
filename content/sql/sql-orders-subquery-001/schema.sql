dbo.Products(ProductId int PK, ProductName nvarchar(80) UNIQUE)
dbo.OrderLines(OrderLineId int PK, ProductId int FK -> Products, Quantity int CHECK > 0)
