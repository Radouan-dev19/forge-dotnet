# Solution expliquée

```sql
WITH Monthly AS (SELECT CONVERT(char(7), OrderDate, 126) AS MonthKey, SUM(Total) AS Total FROM dbo.Orders GROUP BY CONVERT(char(7), OrderDate, 126)) SELECT MonthKey, Total FROM Monthly ORDER BY MonthKey;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
