# Solution expliquée

```sql
SELECT TOP (2) OrderId, Total FROM dbo.Orders ORDER BY Total DESC, OrderId;
```

Borner le résultat sans ordre total ne définit pas quelle ligne sort : deux commandes de même montant pourraient s'échanger d'une exécution à l'autre, et la borne rendrait alors des résultats différents sur une base inchangée. Le second critère de tri est ce qui rend la requête reproductible.
