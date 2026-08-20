# Détecter les services soudés en cycle dans un graphe d'appels

Implémentez `Submission.DeployCycles` avec la signature fournie. Le signe le plus sûr du monolithe
distribué n'est pas la taille des services, c'est le **cycle d'appels** : la facturation appelle le
catalogue qui appelle la facturation. Ces deux services séparés ne se livrent plus séparément, ne se
testent plus séparément et ne tombent plus séparément — ils ont tous les coûts du réseau et aucun
bénéfice du découpage. Votre fonction relève les services pris dans un cycle.

## Le format du graphe

Des arêtes `appelant>appelé` séparées par des points-virgules : `"a>b;b>c;c>a"`.

## Ce qu'il faut produire

Les services **en cycle** — ceux depuis lesquels un chemin d'appels, d'un ou plusieurs sauts,
revient à eux-mêmes — triés par ordre ordinal, joints par des virgules ; la chaîne vide pour un
graphe sans cycle. L'auto-appel `a>a` est le plus petit cycle possible. Attention à la nuance : un
service qui **mène** à un cycle sans en faire partie n'est pas en cycle — l'interface qui appelle
deux services soudés reste librement livrable, elle.

```text
DeployCycles("a>b;b>c;c>a")                  →  "a,b,c"
DeployCycles("a>b;b>c")                      →  ""
DeployCycles("ui>api;api>billing;billing>api") →  "api,billing"
```

## Les refus

`ArgumentException` pour un graphe vide, une arête sans ses deux services, un service sans nom, ou
une arête répétée — un graphe d'appels ne déclare pas deux fois la même dépendance.

## Avant d'écrire

Prédisez le relevé d'un graphe où deux cycles disjoints coexistent, puis d'un graphe en simple
chaîne. Dites ce que l'équipe fait concrètement d'un cycle détecté : lequel des deux appels casse-t-on,
et par quoi le remplace-t-on — événement, réplication de données, ou fusion des services ?
