# Solution expliquée

```sql
SELECT OrderId, OrderDate FROM dbo.Orders WHERE CustomerId = 1 AND OrderDate > '2026-01-15' ORDER BY OrderDate, OrderId;
```

L'égalité sur le client précède l'inégalité sur la date : c'est l'ordre qu'un index composite exploite, la colonne d'égalité restreignant d'abord la plage parcourue. Le second critère de tri départage deux commandes de même date.
