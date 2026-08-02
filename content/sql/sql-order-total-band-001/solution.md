# Solution expliquée

```sql
SELECT OrderId, CASE WHEN Total < 30 THEN N'small' WHEN Total < 80 THEN N'medium' ELSE N'large' END AS Band FROM dbo.Orders ORDER BY OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
