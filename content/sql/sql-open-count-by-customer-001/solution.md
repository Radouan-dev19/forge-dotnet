# Solution expliquée

```sql
SELECT c.CustomerId, SUM(CASE WHEN o.Status = N'Open' THEN 1 ELSE 0 END) AS OpenCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
