# Solution expliquée

```sql
SELECT c.CustomerId, c.Name FROM dbo.Customers c WHERE c.IsActive = 1 AND EXISTS (SELECT 1 FROM dbo.Orders o WHERE o.CustomerId = c.CustomerId AND o.Status = N'Open') ORDER BY c.CustomerId;
```

Le test d'existence répond à « y en a-t-il au moins une ? » sans rapatrier les lignes correspondantes ; une jointure aurait dupliqué le client autant de fois qu'il a de commandes ouvertes. C'est la différence pratique entre filtrer et joindre, sur exactement la même donnée.
