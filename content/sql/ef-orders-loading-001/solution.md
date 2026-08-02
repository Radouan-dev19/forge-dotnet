# Solution expliquée

```csharp
var customers = await context.Customers
    .AsNoTracking()
    .Include(customer => customer.Orders)
    .OrderBy(customer => customer.CustomerId)
    .ToListAsync(cancellationToken);
```

`Include` permet ici une matérialisation en une commande. Le calcul des nombres utilise ensuite les collections déjà chargées, sans nouvelle requête. Pour un volume important, une projection SQL avec `Count` pourrait être encore plus légère ; le scénario isole volontairement le diagnostic du N+1.

Le test compte les commandes réellement exécutées, vérifie les valeurs, puis démontre que le starter en exécute au moins quatre.
