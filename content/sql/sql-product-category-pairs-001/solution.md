# Solution expliquée

```sql
SELECT a.ProductId AS LeftId, b.ProductId AS RightId FROM dbo.Products a JOIN dbo.Products b ON b.Category = a.Category AND b.ProductId > a.ProductId ORDER BY a.ProductId, b.ProductId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
