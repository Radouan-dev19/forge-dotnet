# Explication

Réinitialiser un état de test : rendre un état vierge de même taille, sans toucher l'ancien.
Deux lignes de code, et le sujet le plus déterminant pour la santé d'une suite de tests —
l'*isolation*, que l'énoncé résume par sa question : que fait un état partagé entre deux
tests ?

Il les couple par l'ordre. Le premier test laisse des traces — un compteur incrémenté, une
liste peuplée — et le second, qui hérite de l'état sale, passe ou échoue *selon qu'il tourne
avant ou après*. Les symptômes sont légendaires : la suite verte en local et rouge en
intégration continue, le test qui échoue seulement quand on lance « tous », celui qui guérit
quand on l'exécute seul. Chaque symptôme coûte une enquête, et la cause est toujours la même —
de l'état qui survit d'un test à l'autre. La règle qui protège tient en une phrase : chaque
test commence sur un état *neuf*, jamais sur l'état nettoyé du précédent.

La solution encode cette règle dans sa forme même : elle *fabrique* — `new int[values.Length]`,
un tableau vierge aux valeurs par défaut — au lieu de *nettoyer* — remettre à zéro les cases de
l'existant. La différence semble cosmétique et ne l'est pas : le nettoyage en place mute le
tableau reçu, et tout ce qui détenait une référence — l'autre test, une capture de diagnostic,
une assertion différée — voit ses données effacées sous ses pieds. C'est exactement la
dépendance d'ordre qu'on voulait tuer, recréée par l'outil censé l'empêcher. La fabrication
laisse l'ancien état intact — le harnais le vérifie en comparant l'argument avant et après —
et l'appelant choisit ce qu'il fait de l'ancien : le jeter, l'archiver, le comparer.

La taille conservée est le contrat discret de la fonction : l'état vierge doit être
*structurellement* comparable à l'ancien — même nombre d'emplacements — pour que le test
suivant y trouve la forme attendue. L'état vide rend un état vide neuf, cohérent sans garde ;
le `null` reste une faute d'appel.

Les cas suivent l'énoncé : l'ordinaire remis à zéro, le vide, et la non-mutation vérifiée.

Le coût est linéaire — l'allocation d'un tableau à zéros. La transposition dépasse les
tableaux : bases de données de test recréées plutôt que purgées, conteneurs jetables plutôt
que réutilisés, fixtures reconstruites à chaque test — partout, le même arbitrage entre
*recréer* — sûr, parfois lent — et *nettoyer* — rapide, fragile — et la même préférence par
défaut pour la recréation. Une suite lente se profile et s'optimise ; une suite couplée par
l'ordre ne se fait plus confiance, et une suite en laquelle on ne croit plus ne protège plus
rien.
