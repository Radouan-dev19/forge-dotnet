# Solution expliquée

```csharp
return context.Orders
    .AsNoTracking()
    .Where(order => order.Total >= minimumTotal)
    .OrderBy(order => order.OrderId)
    .Select(order => new OrderSummary(order.OrderId, order.Customer.Name, order.Total));
```

Chaque opérateur enrichit l’arbre d’expression. La matérialisation est laissée à l’appelant ; EF peut donc traduire filtre, jointure, tri et projection en une seule commande paramétrée.

Un passage prématuré par `AsEnumerable` déplacerait le filtre en mémoire. Le test ne recherche pas une chaîne SQL exacte : il vérifie le prédicat, le paramètre, les lignes et les colonnes observables.
