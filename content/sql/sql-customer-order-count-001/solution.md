# Solution expliquée

```sql
SELECT c.CustomerId, COUNT(o.OrderId) AS OrderCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
