# Solution expliquée

```sql
SELECT OrderId, CustomerId FROM dbo.Orders WHERE Status = N'Open' ORDER BY OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
