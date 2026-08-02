# Solution expliquée

```sql
SELECT o.CustomerId, o.Total FROM dbo.Orders o WHERE o.Total = (SELECT MAX(i.Total) FROM dbo.Orders i WHERE i.CustomerId = o.CustomerId) ORDER BY o.CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
