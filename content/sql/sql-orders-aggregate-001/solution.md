# Solution expliquée

```sql
SELECT CustomerId, COUNT_BIG(*) AS OrderCount, SUM(Total) AS TotalAmount
FROM dbo.Orders
WHERE Status IN (N'Paid', N'Pending')
GROUP BY CustomerId
HAVING SUM(Total) >= 20;
```

`WHERE` retire les lignes non facturables avant `COUNT` et `SUM`. `HAVING` travaille ensuite sur chaque groupe. Déplacer le statut dans `HAVING` serait invalide ou changerait la question métier.

La validation non ordonnée compare le multiensemble de lignes : elle accepte un ordre physique différent mais pas un doublon, un groupe supplémentaire ou une somme erronée.
