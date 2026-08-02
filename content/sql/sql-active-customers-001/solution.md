# Solution expliquée

```sql
SELECT CustomerId, Name FROM dbo.Customers WHERE IsActive = 1 ORDER BY CustomerId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
