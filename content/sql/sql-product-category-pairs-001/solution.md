# Solution expliquée

```sql
SELECT a.ProductId AS LeftId, b.ProductId AS RightId FROM dbo.Products a JOIN dbo.Products b ON b.Category = a.Category AND b.ProductId > a.ProductId ORDER BY a.ProductId, b.ProductId;
```

L'inégalité stricte entre les deux identifiants fait tout le travail : elle écarte à la fois la paire d'un produit avec lui-même et le doublon symétrique. Une inégalité simple garderait les deux sens et doublerait le résultat.
