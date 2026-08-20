# Compter les mots d’un texte

Implémente la méthode publique statique `CountWords` de `Submission`. Elle reçoit une chaîne et retourne un dictionnaire associant chaque mot à son nombre d’occurrences. La signature exacte se trouve dans le starter.

Un mot est une suite non vide de caractères pour lesquels `char.IsLetterOrDigit` vaut vrai. Tout autre caractère sépare les mots. Les clés du résultat sont en minuscules invariantes et leur valeur est le nombre d’occurrences. Une chaîne vide ou blanche retourne un dictionnaire vide ; `null` provoque `ArgumentNullException`.

Ne change pas la signature. Pense au dernier mot d’un texte qui ne se termine pas par un séparateur.
