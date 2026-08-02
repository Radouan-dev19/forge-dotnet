# Solution expliquée

```sql
SELECT Label FROM (SELECT City AS Label FROM dbo.Customers UNION SELECT Category FROM dbo.Products) s ORDER BY Label;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
