# Solution expliquée

```sql
SELECT OrderId, OrderDate FROM dbo.Orders WHERE OrderDate >= '2026-02-01' AND OrderDate < '2026-03-01' ORDER BY OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
