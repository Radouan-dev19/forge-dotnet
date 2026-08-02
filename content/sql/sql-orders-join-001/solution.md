# Solution expliquée

```sql
SELECT o.OrderId, c.Name AS CustomerName, o.Total
FROM dbo.Orders AS o
INNER JOIN dbo.Customers AS c ON c.CustomerId = o.CustomerId
WHERE c.IsActive = 1 AND o.Status IN (N'Paid', N'Pending')
ORDER BY o.OrderId;
```

La clé étrangère exprime la relation mais n’ajoute pas automatiquement la jointure dans une requête. Le prédicat `ON` associe chaque commande à son propriétaire ; `WHERE` applique ensuite les règles d’activité et de statut. L’ordre est significatif et fait partie du contrat.

Une jointure sans prédicat peut retourner des colonnes plausibles tout en multipliant les lignes. Les tests comparent donc colonnes, valeurs, ordre et absence d’effet sur le dataset.
