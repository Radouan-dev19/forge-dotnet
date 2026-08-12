# Solution expliquée

```sql
SELECT CustomerId, Name FROM dbo.Customers WHERE IsActive = 1 ORDER BY CustomerId;
```

Le filtre porte sur le drapeau d'activité, pas sur l'existence d'une commande : un client sans commande reste actif s'il est déclaré tel. Trier sur la clé primaire suffit ici à rendre le résultat comparable d'une exécution à l'autre, puisqu'elle est unique.
