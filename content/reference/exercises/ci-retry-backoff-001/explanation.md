# Explication

Le recul exponentiel est une des rares politiques d'exploitation dont tout le monde connaît le nom et
dont presque personne ne sait chiffrer le coût. Or ce chiffre décide de vraies questions : combien
d'exécuteurs immobiliser, quel délai d'expiration poser sur le pipeline entier, à partir de quand une
alerte doit sonner. L'exercice force à passer du slogan — « on double à chaque fois » — au calcul, et
le calcul révèle trois subtilités.

**La première : compter les intervalles, pas les tentatives.** Une campagne de n tentatives contient
n moins une attentes — rien avant la première, rien après la dernière. L'erreur d'une unité ici n'est
pas cosmétique : sur une politique agressive, la dernière attente est la plus longue de toutes, et la
compter à tort gonfle la fenêtre de près du plafond entier. C'est le même décalage que les barrières
et les poteaux d'une clôture, dans un contexte où il se paie en minutes d'exécuteur.

**La deuxième : l'écrêtage change la nature de la croissance.** Sans plafond, la fenêtre double à
chaque tentative — une croissance que personne ne peut budgéter, et qui transforme la dixième relance
en heures d'attente. Le plafond convertit la géométrie en arithmétique : passé le point d'écrêtage,
chaque tentative supplémentaire coûte exactement le plafond, et la fenêtre devient une droite dont la
pente se lit dans la configuration. C'est ce qui rend la politique défendable en réunion
d'exploitation : « au pire, chaque relance au-delà de la quatrième coûte une minute » est une phrase
qu'un budget comprend.

**La troisième : le doublement naïf déborde.** Calculer la puissance de deux d'un coup, puis écrêter,
fonctionne sur les petits cas et déborde en silence dès que l'exposant grandit — et un dépassement
d'entier dans un calcul de délai produit des attentes négatives, donc des relances en rafale,
précisément ce que le recul devait empêcher. Doubler pas à pas en comparant **avant** de doubler ne
déborde jamais, parce que la valeur courante reste toujours sous le plafond. Les bornes du contrat —
tentatives, base, plafond — complètent cette défense en garantissant que la somme elle-même tient
dans le type de retour.

**Les refus comme documentation d'exploitation.** Chaque borne encode un jugement : plus de cent
tentatives ne masquent plus un incident passager mais une panne qu'il faut voir ; un plafond au-delà
d'une journée ne décrit plus une attente mais un abandon ; un plafond sous le délai de base est une
contradiction interne de la configuration. Refuser ces valeurs au seuil de la fonction, c'est refuser
qu'une configuration absurde produise un chiffre plausible.

La transposition dépasse les pipelines : clients de bases de données, appels de services externes,
consommateurs de files — toute politique de relance se budgète avec exactement ce calcul, et toute
revue de politique de relance devrait commencer par lui.
