# Solution expliquée

```sql
SELECT DISTINCT City FROM dbo.Customers ORDER BY City;
```

Éliminer les répétitions et regrouper produisent ici le même résultat, et la lisibilité tranche : on ne calcule rien par groupe, donc l'intention est bien de dédupliquer. Le tri est indispensable, car l'unicité n'implique aucun ordre.
