# Explication

Ce petit agrégat LINQ enseigne la mécanique qui fait tout l'intérêt du style requête : la
composition *sans* matérialisation. Le comprendre ici, sur deux opérateurs, immunise contre les
chaînes pathologiques qu'on rencontre en maintenance.

`Where` ne produit pas une collection : il produit une *description* — « les éléments pairs de
cette source » — qui ne coûte rien tant que personne ne la consomme. `Sum` est le consommateur :
il tire les éléments un à un à travers le filtre et cumule au fil de l'eau. Résultat, la
séquence est énumérée exactement une fois, aucune collection intermédiaire n'existe, et la
mémoire consommée est constante quelle que soit la taille de l'entrée. C'est précisément ce que
le contrat demande — « sans énumérer la séquence plusieurs fois » — et c'est ce que casse la
version `Where(...).ToArray().Sum()` ou, pire, `ToList()` : un tableau entier alloué pour être
détruit aussitôt sommé. Sur un tableau de dix entiers, personne ne le voit ; dans un service qui
traite des lots, ces allocations intermédiaires deviennent la première ligne du profileur.
Apprendre à repérer la matérialisation sans usage est l'objectif réel de l'exercice.

Le prédicat de parité reprend la forme robuste `value % 2 == 0`, correcte sur les négatifs — le
reste de moins quatre par deux est zéro — et sur zéro lui-même, qui est pair. La somme des pairs
d'un tableau qui n'en contient aucun vaut zéro : c'est le neutre de l'addition que `Sum` rend
sur une séquence vide, et le filtre qui ne laisse rien passer y conduit naturellement — aucune
garde spéciale, le comportement de bord découle des définitions. Même chose pour le tableau
vide en entrée. Le `null`, lui, reste une faute d'appel signalée avant la chaîne : LINQ lèverait
aussi, mais plus tard et avec un message moins net — valider tôt donne la meilleure erreur.

Une nuance honnête sur les débordements : `Sum` sur des `int` cumule en `int` et lève en cas de
dépassement — il est vérifié en interne — ce qui est le comportement sain ; les cas de cet
exercice restent loin des limites.

La transposition est un réflexe de lecture : devant toute chaîne LINQ, chercher où elle se
matérialise. La règle saine tient en deux mots — *à la fin*, et seulement si l'appelant a
besoin d'une collection. Un agrégat n'en a jamais besoin : `Sum`, `Count`, `Any`, `Max`
consomment le flux directement, et chaque `ToList` posé avant eux est un coût sans contrepartie
qu'une revue doit savoir nommer.
