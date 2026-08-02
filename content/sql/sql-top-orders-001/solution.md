# Solution expliquée

```sql
SELECT TOP (2) OrderId, Total FROM dbo.Orders ORDER BY Total DESC, OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
