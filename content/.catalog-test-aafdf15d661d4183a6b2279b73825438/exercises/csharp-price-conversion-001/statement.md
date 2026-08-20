# Convertir un prix en centimes

Implémente la méthode suivante :

```csharp
public static int ToCents(decimal amount)
```

`amount` représente un montant en euros compris entre `0m` et `1_000_000m`. Retourne le nombre entier de centimes. Un demi-centime est arrondi en s’éloignant de zéro : `10.005m` produit donc `1001`.

Un montant négatif est invalide et doit provoquer `ArgumentOutOfRangeException`. Ne change ni le nom de la classe `Submission`, ni la signature publique.

Avant de coder, explique pourquoi convertir trop tôt en `int` donnerait un résultat faux.
