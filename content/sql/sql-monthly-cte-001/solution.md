# Solution expliquée

```sql
WITH Monthly AS (SELECT CONVERT(char(7), OrderDate, 126) AS MonthKey, SUM(Total) AS Total FROM dbo.Orders GROUP BY CONVERT(char(7), OrderDate, 126)) SELECT MonthKey, Total FROM Monthly ORDER BY MonthKey;
```

Nommer l'agrégation dans une expression de table rend la requête relisible sans changer son plan : le moteur la traite comme une sous-requête. Le format de clé retenu place l'année avant le mois, ce qui fait coïncider l'ordre alphabétique et l'ordre chronologique.
