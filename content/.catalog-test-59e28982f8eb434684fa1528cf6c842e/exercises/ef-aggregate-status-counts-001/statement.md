# Agréger les commandes par statut avec un filtre de groupe côté serveur

Implémentez `Submission.StatusCounts` avec la signature fournie. Le squelette monte une base SQLite
en mémoire — six commandes réparties sur trois statuts — et ouvre un contexte dessus ; votre travail
est la requête d'agrégation. Le tableau de bord d'exploitation veut le nombre de commandes par
statut, mais seulement pour les statuts qui atteignent un plancher d'occurrences : les autres sont du
bruit d'affichage.

## Les données

| Statut | Commandes |
|---|---|
| shipped | 3 |
| pending | 2 |
| canceled | 1 |

## Ce qu'il faut produire

Un dictionnaire statut vers compte, limité aux statuts dont le compte atteint le plancher.

```text
StatusCounts(2)  →  { "pending": 2, "shipped": 3 }
StatusCounts(3)  →  { "shipped": 3 }
StatusCounts(4)  →  { }
```

## La question se pose sur les groupes

« Au moins deux occurrences » ne se vérifie sur aucune ligne : c'est une propriété du **groupe**, qui
n'existe qu'après le regroupement. Le filtre doit donc se poser après lui — c'est l'équivalent du
`having` relationnel, et le fournisseur le traduit ainsi côté serveur. Posé avant le regroupement, le
même prédicat filtrerait des lignes individuelles et répondrait à une question différente.

Comme toujours avec un contexte : le regroupement, le comptage et le filtre s'exécutent dans la
requête. Rapatrier les six commandes pour les regrouper en mémoire donne le même dictionnaire ici, et
un transfert de table entière en production.

## Les refus

`ArgumentOutOfRangeException` pour un plancher nul ou négatif : tout statut l'atteindrait et le
filtre ne filtrerait rien.

## Avant d'écrire

Prédisez le dictionnaire pour un plancher de un, puis pour un plancher que même le statut le plus
fréquent n'atteint pas. Dites pourquoi un dictionnaire vide est ici une réponse et non une erreur.
