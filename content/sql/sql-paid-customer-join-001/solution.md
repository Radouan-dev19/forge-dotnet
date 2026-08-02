# Solution expliquée

```sql
SELECT o.OrderId, c.Name FROM dbo.Orders o JOIN dbo.Customers c ON c.CustomerId = o.CustomerId WHERE o.Status = N'Paid' ORDER BY o.OrderId;
```

La requête fixe le grain avant projection, borne le résultat et utilise uniquement le schéma visible. Sa preuve compare colonnes, lignes et ordre, jamais un coût ou une durée exacte.
