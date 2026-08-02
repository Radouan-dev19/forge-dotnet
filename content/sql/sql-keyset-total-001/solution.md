# Solution expliquée

```sql
SELECT TOP (2) OrderId, Total FROM dbo.Orders WHERE Total > 70 OR (Total = 70 AND OrderId > 3) ORDER BY Total, OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
