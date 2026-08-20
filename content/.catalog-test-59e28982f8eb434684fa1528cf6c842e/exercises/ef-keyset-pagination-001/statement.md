# Paginer par jeu de clés depuis un curseur d'identifiant

Implémentez `Submission.NextPage` avec la signature fournie. Le squelette monte une base SQLite en
mémoire — sept commandes insérées en désordre — et ouvre un contexte dessus ; votre travail est la
requête de pagination. Le client parcourt la liste page par page, et chaque page se demande par un
**curseur** : le dernier identifiant déjà lu.

## Les données

Les identifiants présents : 3, 8, 11, 14, 21, 27, 35 — insérés dans un ordre quelconque, comme dans
toute vraie table.

## Ce qu'il faut produire

Les identifiants de la page suivante : ceux **strictement supérieurs** au curseur, en ordre croissant,
limités à la taille de page. Un tableau vide signale la fin du parcours.

```text
NextPage(0, 3)    →  [3, 8, 11]
NextPage(11, 3)   →  [14, 21, 27]
NextPage(35, 5)   →  []
```

## Pourquoi le jeu de clés et pas le décalage

La pagination par décalage numérique — sauter n lignes — recompte depuis le début à chaque page :
une insertion ou une suppression pendant le parcours décale tout, et le lecteur voit des doublons ou
des trous. Le curseur, lui, dit « après cette ligne-ci », une position que les écritures concurrentes
ne déplacent pas. Le serveur saute directement au curseur par l'index de la clé au lieu de parcourir
les lignes sautées. Deux pièges gardent l'exercice honnête : la comparaison est **stricte** — le
curseur a déjà été lu — et l'ordre doit être posé **dans la requête**, avant la limitation, faute de
quoi la base tronque un ordre d'arrivée que rien ne garantit.

## Les refus

`ArgumentOutOfRangeException` pour un curseur négatif — les identifiants commencent à un, zéro est le
curseur de départ — et pour une taille de page hors de un à cinquante : une page démesurée est un
contournement de la pagination, pas une page.

## Avant d'écrire

Prédisez la page pour un curseur posé exactement sur un identifiant existant, puis pour un curseur
entre deux identifiants. Dites pourquoi le second cas fonctionne sans traitement particulier — et ce
que cette propriété change après une suppression.
