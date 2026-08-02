# Solution expliquée

```sql
SELECT CustomerId, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY CustomerId HAVING COUNT_BIG(*) >= 2 ORDER BY CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
