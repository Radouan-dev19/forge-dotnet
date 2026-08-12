# Solution expliquée

```sql
SELECT OrderId, CustomerId FROM dbo.Orders WHERE Status = N'Open' ORDER BY OrderId;
```

Le statut se compare à un littéral national, comme la colonne le stocke : mélanger les deux formes de littéral force une conversion qui empêche l'usage d'un index. C'est une des causes les plus discrètes de balayage complet sur une colonne pourtant indexée.
