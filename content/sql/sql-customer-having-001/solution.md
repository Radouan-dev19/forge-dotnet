# Solution expliquée

```sql
SELECT CustomerId, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY CustomerId HAVING COUNT_BIG(*) >= 2 ORDER BY CustomerId;
```

Le filtre s'applique après le regroupement, ce qui est la seule position possible : le nombre de commandes d'un client n'existe pas avant que le groupe ne soit formé. Un filtre placé avant le regroupement porterait sur une ligne isolée et répondrait à une autre question.
