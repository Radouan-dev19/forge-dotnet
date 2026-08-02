# Solution expliquée

```sql
SELECT CustomerId, SUM(Total) AS Revenue FROM dbo.Orders GROUP BY CustomerId ORDER BY CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
