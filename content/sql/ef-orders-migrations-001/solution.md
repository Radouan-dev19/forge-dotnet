# Solution expliquée

```csharp
await context.Database.MigrateAsync(cancellationToken);
```

`MigrateAsync` compare l’historique aux migrations compilées puis applique uniquement les migrations absentes. À l’inverse, `EnsureCreatedAsync` contourne le mécanisme de migrations et ne convient pas pour démontrer l’évolution d’un schéma.

La migration crée d’abord `Customers`, puis `Orders` et sa clé étrangère. L’ordre inverse échouerait parce que la table principale n’existerait pas. Le rollback pédagogique supprime d’abord `Orders`, puis `Customers`, puis l’historique.

Le test réel appelle deux fois la solution, compte l’historique, inspecte la clé étrangère et les index, puis exécute le reset. Il ne compare aucun coût de plan et ne dépend d’aucune chaîne de connexion écrite dans le contenu.
