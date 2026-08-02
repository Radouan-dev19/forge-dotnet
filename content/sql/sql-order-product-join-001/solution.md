# Solution expliquée

```sql
SELECT p.Name, l.Quantity FROM dbo.OrderLines l JOIN dbo.Products p ON p.ProductId = l.ProductId WHERE l.OrderId = 1 ORDER BY p.ProductId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
