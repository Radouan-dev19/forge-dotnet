# Solution expliquée

```csharp
try
{
    await secondContext.SaveChangesAsync(cancellationToken);
    return false;
}
catch (DbUpdateConcurrencyException)
{
    return true;
}
```

EF inclut la valeur originale de `RowVersion` dans le prédicat de l’`UPDATE`. La première sauvegarde change cette valeur ; la seconde ne touche donc aucune ligne et EF lève l’exception dédiée.

Le starter ne gère pas cette exception. Le test négatif exige qu’elle remonte, tandis que la solution la transforme en résultat explicite. Le reset est contrôlé après chaque variante afin qu’une exécution ne pollue jamais la suivante.
