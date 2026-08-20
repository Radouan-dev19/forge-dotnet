# Localiser le vrai coupable d'une trace lente par le temps propre

Implémentez `Submission.SlowestSpan` avec la signature fournie. Dans une trace distribuée, le segment
le plus long est rarement le coupable : la passerelle « dure » deux cents millisecondes parce
qu'elle attend ses appels. Le vrai coupable se juge au **temps propre** — ce qui reste d'une durée
une fois retiré ce que ses enfants expliquent. Votre fonction le désigne.

## Le format de la trace

Des segments `nom:début:fin:parent` séparés par des points-virgules, le parent `-` marquant la
racine. Les instants sont en millisecondes depuis le début de la requête, dans un ordre de journal
quelconque.

## Ce qu'il faut produire

Le nom du segment au **temps propre maximal**, suivi de la barre verticale et de ce temps propre :
`durée du segment − somme des durées de ses enfants directs`. En cas d'égalité, le début le plus
précoce l'emporte, puis l'ordre ordinal des noms — un rapport rend le même verdict à chaque
exécution.

```text
SlowestSpan("gateway:0:200:-;auth:10:40:gateway;orders:40:180:gateway;db:60:170:orders")
  →  "db|110"
SlowestSpan("api:0:100:-")
  →  "api|100"
```

Le premier exemple raconte le piège : la passerelle porte 200 ms, mais 170 s'écoulent dans ses
appels — son temps propre est de 30 ms, comme celui de l'authentification et de la couche des
commandes. C'est la base de données, à 110 ms de travail en propre, qui mérite l'enquête.

Les enfants **directs** seulement : soustraire toute la descendance compterait deux fois le temps
des petits-enfants, déjà retiré de leurs parents.

## Les refus

`ArgumentException` pour une trace vide, un segment illisible, une fin avant le début, un nom de
segment répété, ou un parent cité qui n'existe pas — une trace aux liens cassés relève du comptage
d'orphelins, pas de l'analyse de chemin.

## Avant d'écrire

Prédisez le verdict d'une trace où la racine n'attend presque rien et où deux enfants se partagent
exactement le même temps propre. Dites pourquoi la règle de départage fait partie du contrat au même
titre que le calcul.
