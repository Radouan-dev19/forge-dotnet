# Solution expliquée

```sql
SELECT c.CustomerId, SUM(CASE WHEN o.Status = N'Open' THEN 1 ELSE 0 END) AS OpenCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;
```

Le filtre sur le statut se place dans l'expression agrégée, pas dans la clause de filtrage : déplacé au filtre, il éliminerait les clients sans commande ouverte alors qu'ils doivent afficher zéro. Le comptage conditionnel garde donc le groupe tout en ne comptant que ce qui qualifie.
