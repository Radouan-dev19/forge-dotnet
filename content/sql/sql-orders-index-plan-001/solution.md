# Solution expliquée

```sql
SELECT COUNT_BIG(*) AS MatchingOrders
FROM dbo.Orders
WHERE CustomerId = 777
  AND CreatedAtUtc >= '2026-01-14';
```

L’égalité sur la première clé et la plage sur la seconde correspondent à l’ordre de l’index. La date littérale ISO est non ambiguë. L’assertion de plan se limite au nom de l’index et à une opération de recherche ; un coût exact serait une preuve fragile.

Un index accélère certaines lectures au prix d’espace et d’écritures supplémentaires. Le scénario n’affirme donc pas qu’il faut indexer toutes les colonnes filtrées.
