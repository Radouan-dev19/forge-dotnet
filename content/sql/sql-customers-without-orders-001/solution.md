# Solution expliquée

```sql
SELECT c.CustomerId, c.Name FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId WHERE o.OrderId IS NULL ORDER BY c.CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
