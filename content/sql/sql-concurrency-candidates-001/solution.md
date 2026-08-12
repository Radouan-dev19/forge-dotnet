# Solution expliquée

```sql
SELECT OrderId, DataVersion FROM dbo.Orders WHERE Status = N'Open' ORDER BY OrderId;
```

Lire la version est le premier temps de la concurrence optimiste : la valeur relevée ici devient la condition de la mise à jour qui suivra, et son absence dans la clause de filtrage expliquerait une mise à jour perdue. Une lecture n'incrémente jamais le jeton.
