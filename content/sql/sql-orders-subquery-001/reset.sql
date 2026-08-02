DELETE FROM dbo.OrderLines;
DELETE FROM dbo.Products;
INSERT dbo.Products VALUES (1,N'Keyboard'),(2,N'Mouse'),(3,N'Screen'),(4,N'Dock');
INSERT dbo.OrderLines VALUES (1,1,2),(2,2,1),(3,4,3);
