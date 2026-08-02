# Solution expliquée

```csharp
Order first = await context.Orders.SingleAsync(x => x.OrderId == orderId);
Order second = await context.Orders.SingleAsync(x => x.OrderId == orderId);
Order readOnly = await context.Orders.AsNoTracking().SingleAsync(x => x.OrderId == orderId);
```

Le cache d’identité du contexte renvoie la même instance pour les deux lectures suivies. La troisième entité n’entre pas dans le `ChangeTracker`; son état observé est `Detached`. Le test vérifie également qu’une seule entrée reste suivie et que la table n’a pas changé après le reset.

Erreur fréquente : confondre absence de tracking et absence de requête SQL. `AsNoTracking` exécute bien une requête ; il change uniquement la gestion des entités matérialisées.
