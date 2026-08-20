# Compter les jours ouvrés

Implémente `public static int CountBusinessDays(DateOnly start, DateOnly end)`.

Compte les dates du lundi au vendredi entre les deux bornes incluses. Les jours fériés ne font pas partie de cet exercice. Si `start > end`, retourne `0`.

Utilise `DateOnly.DayOfWeek` et `AddDays`. Avant de coder, liste les résultats attendus pour un samedi seul, un lundi seul et une plage du vendredi au lundi.
