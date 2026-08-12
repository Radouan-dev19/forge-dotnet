# Solution expliquée

```sql
SELECT o.OrderId, c.Name FROM dbo.Orders o JOIN dbo.Customers c ON c.CustomerId = o.CustomerId WHERE o.Status = N'Paid' ORDER BY o.OrderId;
```

La jointure interne convient parce que la clé étrangère est obligatoire : aucune commande ne peut exister sans client, donc aucune ligne n'est perdue. Le même choix serait faux sur une relation facultative, où il masquerait silencieusement des commandes.
