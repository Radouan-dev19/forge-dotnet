# Composer le statut et les en-têtes obligatoires d'une réponse

Implémentez `Submission.ResponseContract` avec la signature fournie. Un statut HTTP seul est souvent
une réponse incomplète : une création sans adresse, une redirection sans cible ou un étranglement
sans délai laissent le client deviner la suite. Votre fonction compose, pour une requête décrite, le
statut **et** les en-têtes que ce statut rend obligatoires.

## Le format de la requête

La requête se décrit par trois paires clé-valeur, jointes par des points-virgules et livrées sans
ordre garanti :

- `method` — `get`, `post`, `put` ou `delete` ;
- `state` — l'état de la ressource visée : `present`, `absent`, `gone` (disparue définitivement,
  pierre tombale) ou `moved` (déplacée de façon permanente) ;
- `load` — `normal` ou `throttled`.

## Le contrat de réponse

Rendez le statut, suivi de `|` et du nom d'en-tête quand le statut l'exige. Les règles, par étage :

1. `load=throttled` → `429|Retry-After`, quoi qu'il arrive : refuser sans dire quand revenir
   fabrique des tempêtes de relances.
2. `state=moved` → `301|Location` pour `get`, `308|Location` pour les autres méthodes — la
   redirection historique autorise le client à changer de méthode, ce qu'une écriture ne survit pas.
3. `state=gone` → `410` pour toute méthode : la pierre tombale dit « n'insistez plus », là où `404`
   laisse espérer.
4. Le croisement restant : `get` rend `200` sur `present` et `404` sur `absent` ; `put` rend `204`
   sur `present` et `201|Location` sur `absent` ; `delete` rend `204` sur `present` et `404` sur
   `absent` ; `post` rend `409` sur `present` — la représentation existe déjà — et `201|Location`
   sur `absent`.

```text
ResponseContract("method=get;state=present;load=normal")   →  "200"
ResponseContract("method=post;state=absent;load=normal")   →  "201|Location"
ResponseContract("method=get;state=absent;load=throttled") →  "429|Retry-After"
```

## Les refus

`ArgumentException` pour une paire illisible, une clé hors des trois attendues, une clé répétée, une
valeur hors vocabulaire ou un attribut manquant.

## Avant d'écrire

Prédisez la réponse d'un `put` vers une ressource déplacée, puis d'un `delete` vers une ressource
disparue définitivement. Dites, pour chacune, ce que le client est censé faire ensuite — c'est le
critère qui départage les statuts voisins.
