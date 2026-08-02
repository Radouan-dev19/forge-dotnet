CREATE TABLE dbo.Inventory (ProductId int PRIMARY KEY, Quantity int NOT NULL CHECK (Quantity >= 0));
INSERT dbo.Inventory VALUES (1,5),(2,1);
