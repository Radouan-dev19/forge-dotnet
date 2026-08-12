# Explication recopiée

## Intuition

Une chaîne LINQ décrit un filtrage, une projection puis une agrégation successives.

## Explication

Une chaîne LINQ décrit un filtrage, une projection puis une agrégation successives. La règle doit
rester visible dans le nom des opérations, les bornes et les erreurs.

```csharp
int[] positifs = valeurs.Where(valeur => valeur > 0).ToArray();
```
