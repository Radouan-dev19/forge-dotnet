# Explication

Sommer une plage d'entiers avec une boucle : l'énoncé impose même l'outil, et cette contrainte
dit le vrai sujet — l'exercice ne porte pas sur la somme, il porte sur la capacité à énoncer ce
qu'une boucle garantit à chaque instant.

L'invariant demandé s'écrit en une phrase : après l'itération qui traite `current`, `total`
vaut la somme des entiers de `start` à `current` inclus. Cette phrase prouve la boucle entière.
À l'entrée du premier tour, `total` vaut zéro — la somme de la plage vide avant `start`. Chaque
tour étend la propriété d'une unité. À la sortie, `current` a dépassé `end`, donc `total` vaut
la somme jusqu'à `end` : exactement le contrat. Savoir dérouler ce raisonnement à voix haute
transforme les boucles de « ça a l'air de marcher » en « voilà pourquoi c'est juste », et c'est
une compétence d'entretien autant que de revue.

Les bornes concentrent les décisions. « Inclusives » se lit dans la condition `current <= end` —
le `<` strict amputerait la somme de son dernier terme, l'erreur d'un caractère que le cas d'une
plage à un seul entier — `start` égal à `end` — expose immédiatement : la réponse est cet
entier, pas zéro. L'intervalle inversé, lui, relève de la convention, et le contrat choisit
zéro — la somme d'aucun terme — matérialisée par une garde en tête plutôt que par une boucle
qui ne tournerait pas : les deux seraient équivalentes ici, mais la garde documente le cas et
survivrait à un changement de forme de boucle. Les valeurs négatives traversent sans traitement
spécial — une plage de moins cinq à trois se somme comme une autre, et le cas caché qui
chevauche zéro le vérifie.

Il existe une réponse en temps constant — la formule de la somme d'une progression
arithmétique — et l'exercice l'interdit sciemment. Le choix n'est pas anti-mathématique : la
formule exige de raisonner sur les débordements du produit avant division, et surtout l'objectif
pédagogique est l'invariant, pas l'astuce. Dans du code réel, la formule gagnerait dès que la
plage devient grande ; les bornes de l'énoncé — mille en valeur absolue — disent explicitement
que le résultat tient dans un `int`, ce qui autorise le cumul direct sans vérification.

La transposition dépasse largement les sommes : toute boucle d'accumulation — total d'un panier,
concaténation de segments, fusion d'intervalles — se spécifie par la même phrase « après le tour
k, l'accumulateur vaut... ». Écrire cette phrase *avant* la boucle, comme l'énoncé l'exige, est
l'habitude qui distingue le code prouvé du code essayé.
