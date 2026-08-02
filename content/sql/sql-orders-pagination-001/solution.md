# Solution expliquée

```sql
SELECT TOP (3) OrderId, CreatedAtUtc, Total
FROM dbo.Orders
WHERE CreatedAtUtc > '2026-07-02T09:00:00'
   OR (CreatedAtUtc = '2026-07-02T09:00:00' AND OrderId > 3)
ORDER BY CreatedAtUtc, OrderId;
```

Le curseur reprend toutes les colonnes de l’ordre. `OrderId` départage les dates égales et rend l’ordre total. `TOP (3)` borne le résultat sans parcourir les pages précédentes.

Le test compare les trois lignes et l’ordre, puis insère une commande antérieure dans une transaction : les mêmes identifiants doivent rester visibles avant rollback.
