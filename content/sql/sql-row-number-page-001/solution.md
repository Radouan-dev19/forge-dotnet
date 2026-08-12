# Solution expliquée

```sql
WITH Ranked AS (SELECT OrderId, ROW_NUMBER() OVER (ORDER BY OrderDate, OrderId) AS Position FROM dbo.Orders) SELECT OrderId, Position FROM Ranked WHERE Position BETWEEN 2 AND 4 ORDER BY Position;
```

Le rang ne peut pas être filtré là où il est calculé : la fonction de fenêtrage est évaluée après la clause de filtrage, d'où le passage par une expression de table nommée. C'est aussi ce qui rend cette pagination coûteuse, chaque page exigeant de numéroter tout ce qui précède.
