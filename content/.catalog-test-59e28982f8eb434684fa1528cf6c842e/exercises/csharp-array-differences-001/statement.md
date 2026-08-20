# Calculer les écarts successifs

Implémente `public static int[] Differences(int[] values)`.

Pour chaque paire voisine, retourne `values[i + 1] - values[i]`. L’entrée `[3, 8, 6, 10]` produit `[5, -2, 4]`. Un tableau vide ou d’un seul élément produit un nouveau tableau vide. `null` doit provoquer `ArgumentNullException`.

La méthode ne doit pas modifier `values`. Dessine les index lus et écrit pour une entrée de quatre valeurs avant de coder.
