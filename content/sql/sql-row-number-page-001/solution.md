# Solution expliquée

```sql
WITH Ranked AS (SELECT OrderId, ROW_NUMBER() OVER (ORDER BY OrderDate, OrderId) AS Position FROM dbo.Orders) SELECT OrderId, Position FROM Ranked WHERE Position BETWEEN 2 AND 4 ORDER BY Position;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
