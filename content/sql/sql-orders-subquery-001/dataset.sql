CREATE TABLE dbo.Products (ProductId int PRIMARY KEY, ProductName nvarchar(80) NOT NULL UNIQUE);
CREATE TABLE dbo.OrderLines (OrderLineId int PRIMARY KEY, ProductId int NOT NULL REFERENCES dbo.Products(ProductId), Quantity int NOT NULL CHECK (Quantity > 0));
INSERT dbo.Products VALUES (1,N'Keyboard'),(2,N'Mouse'),(3,N'Screen'),(4,N'Dock');
INSERT dbo.OrderLines VALUES (1,1,2),(2,2,1),(3,4,3);
