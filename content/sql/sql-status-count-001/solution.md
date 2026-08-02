# Solution expliquée

```sql
SELECT Status, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY Status ORDER BY Status;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
