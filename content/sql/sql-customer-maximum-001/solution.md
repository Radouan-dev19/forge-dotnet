# Solution expliquée

```sql
SELECT o.CustomerId, o.Total FROM dbo.Orders o WHERE o.Total = (SELECT MAX(i.Total) FROM dbo.Orders i WHERE i.CustomerId = o.CustomerId) ORDER BY o.CustomerId;
```

La sous-requête est corrélée : elle est évaluée pour chaque commande examinée, avec le client de cette commande. Une sous-requête non corrélée donnerait le maximum global et ne retiendrait qu'un seul client, ce qui est la confusion la plus fréquente sur cette forme.
