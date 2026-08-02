# Solution expliquée

```sql
SELECT c.CustomerId, c.Name FROM dbo.Customers c WHERE c.IsActive = 1 AND EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.CustomerId = c.CustomerId AND o.Status = N'Open') ORDER BY c.CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
