# Solution expliquée

```sql
SELECT c.CustomerId, COUNT(o.OrderId) AS OrderCount FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId GROUP BY c.CustomerId ORDER BY c.CustomerId;
```

Compter la colonne de la table jointe, et non les lignes, est ce qui produit zéro plutôt qu'un : une jointure externe fabrique une ligne pour un client sans commande, mais la colonne rapportée y est absente et n'est donc pas comptée. Compter les lignes afficherait un partout.
