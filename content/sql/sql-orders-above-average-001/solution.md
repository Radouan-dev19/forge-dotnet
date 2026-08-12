# Solution expliquée

```sql
SELECT OrderId, Total FROM dbo.Orders WHERE Total > (SELECT AVG(Total) FROM dbo.Orders) ORDER BY OrderId;
```

La sous-requête n'est pas corrélée : elle produit une valeur unique, évaluée une fois, et comparable à chaque ligne. C'est ce qui la distingue du maximum par client, où la sous-requête doit recevoir la ligne courante pour répondre.
