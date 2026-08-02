# Solution expliquée

```sql
SELECT OrderId, SUM(Total) OVER (ORDER BY OrderId ROWS UNBOUNDED PRECEDING) AS RunningTotal FROM dbo.Orders ORDER BY OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
