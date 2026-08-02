# Solution expliquée

```sql
SELECT OrderId, OrderDate FROM dbo.Orders WHERE CustomerId = 1 AND OrderDate > '2026-01-15' ORDER BY OrderDate, OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
