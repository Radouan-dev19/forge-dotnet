# Solution expliquée

```sql
SELECT c.CustomerId, c.Name FROM dbo.Customers c LEFT JOIN dbo.Orders o ON o.CustomerId = c.CustomerId WHERE o.OrderId IS NULL ORDER BY c.CustomerId;
```

L'anti-jointure repose sur une jointure externe suivie d'un test d'absence sur la clé de la table jointe : cette clé ne peut être absente que si aucune ligne n'a été appariée. Tester une colonne annulable de cette table casserait le raisonnement, car son absence de valeur ne prouverait rien.
