# Retrouver les clients sans grosse commande sans perdre les clients sans commande

Implémentez `Submission.SilentCustomers` avec la signature fournie. Le squelette monte une base
SQLite en mémoire — quatre clients, quatre commandes — et ouvre un contexte dessus ; votre travail
est la requête. Le service marketing veut la liste des clients qui n'ont **aucune** commande
atteignant le seuil donné, pour les relancer.

## Les données

| Client | Commandes (totaux) |
|---|---|
| Ada | 120.50 et 40.25 |
| Grace | 75.0 |
| Linus | 15.0 |
| Margaret | aucune |

## Ce qu'il faut produire

Les noms des clients cibles, triés par la requête, joints par des virgules.

```text
SilentCustomers(100)  →  "Grace,Linus,Margaret"
SilentCustomers(70)   →  "Linus,Margaret"
SilentCustomers(15)   →  "Margaret"
```

## Le piège de la jointure

Margaret est la cliente la plus silencieuse de toutes — et c'est exactement elle qu'une jointure
interne fait disparaître : sans commande, elle n'a aucune ligne à joindre, donc aucune ligne à
filtrer. La question « qui n'a pas de commande qualifiante » se pose depuis l'ensemble des clients,
avec une **négation d'existence** sur la propriété de navigation, que le fournisseur traduit en
sous-requête côté serveur. Partir des commandes répond à une autre question.

La contrainte d'exécution compte autant que le résultat : la sélection, le tri et la projection
restent dans la requête. Charger les quatre clients et filtrer en mémoire donne la même chaîne sur
cette base de démonstration — et un transfert intégral de table sur une base réelle.

## Les refus

`ArgumentOutOfRangeException` pour un seuil nul ou négatif : toute commande l'atteindrait, et la
question de la relance se viderait de sens.

## Avant d'écrire

Prédisez le résultat pour un seuil exactement égal au total d'une commande de Grace, puis pour un
seuil que même Ada n'atteint pas. Dites, dans chaque cas, de quel côté de la comparaison la frontière
tombe.
