# Explication

Une moyenne, c'est une division — et tout ce que cet exercice enseigne tourne autour de ce que la
division peut détruire si on la laisse faire.

Le point central est le type des opérandes. `sum / values.Length` avec deux entiers exécute une
division *entière* : la moyenne de un, deux et trois serait deux — juste, par chance — mais celle
de un et deux serait un, le reste jeté sans avertissement. La solution convertit la somme en
`decimal` avant de diviser, et la position de cette conversion est le cœur du sujet : convertir
le *résultat* d'une division entière, `(decimal)(sum / count)`, arrondit d'abord et décore
ensuite — le mal est fait. La règle se retient ainsi : le type d'une expression se décide au
moment où l'opérateur s'applique, pas au moment où on lit le résultat. Les cas cachés placent des
moyennes non entières précisément pour départager les deux écritures.

Deuxième décision : l'accumulateur en `long`. Chaque valeur tient dans un `int`, mais leur somme
n'a aucune raison d'y tenir — mille valeurs proches du maximum débordent largement. Élargir
l'accumulateur repousse le problème hors du domaine plausible au lieu de le vérifier à chaque
addition : c'est l'autre stratégie face au débordement, complémentaire du `checked` utilisé dans
les exercices de cumul. Élargir quand un type plus grand existe et suffit ; vérifier quand on
est déjà au plus large. Savoir énoncer ce choix vaut mieux que d'appliquer l'un des deux par
réflexe.

Troisième décision : la collection vide. Diviser par zéro d'éléments n'a pas de sens
mathématique, et le contrat tranche par une convention explicite — zéro — plutôt que par
l'exception que lèverait `Average` de LINQ sur une séquence vide. Aucune des deux conventions
n'est universellement bonne : zéro simplifie les agrégations de tableaux de bord, l'exception
protège les calculs où « pas de données » doit arrêter le traitement. Ce que l'exercice impose,
c'est que la convention soit écrite dans le nom même de la méthode — `AverageOrZero` — et
couverte par un cas. Une convention non nommée est un bug en attente de découverte.

Le coût est un parcours linéaire et une division ; rien à discuter. La transposition, elle, est
omniprésente : panier moyen, temps de réponse moyen, note moyenne. Chaque fois, les trois mêmes
questions — division entière ou décimale, capacité de l'accumulateur, définition du cas vide —
et chaque fois, une réponse à écrire noir sur blanc avant de coder. L'exercice est le gabarit de
cette rédaction.
