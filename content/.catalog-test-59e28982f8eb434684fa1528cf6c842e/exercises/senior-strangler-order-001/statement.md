# Ordonner l'extraction des modules d'un système hérité

Implémentez `Submission.ExtractionOrder` avec la signature fournie. La stratégie de l'étrangleur
remplace un système hérité **morceau par morceau** : chaque module extrait vit sa nouvelle vie
pendant que le reste continue de tourner. La stratégie tient ou casse sur une décision : **dans quel
ordre extraire**. Votre fonction produit ce plan depuis la description des modules.

## Le format de la description

Des modules `nom:entrantes:sortantes` séparés par des points-virgules — les dépendances entrantes
(qui l'appelle) et sortantes (qui il appelle), en nombre.

## Le plan

Tous les modules, triés par :

1. dépendances **entrantes** croissantes — chaque entrante est un appelant à repointer le jour de
   l'extraction : le moins appelé coûte le moins cher, et les premières extractions bon marché
   forment l'équipe avant les chantiers durs ;
2. à égalité, dépendances **sortantes** croissantes — le module qui dépend peu des autres se teste
   plus facilement une fois seul ;
3. à égalité encore, l'ordre ordinal des noms.

```text
ExtractionOrder("billing:0:3;catalog:2:1;auth:5:0")  →  "billing;catalog;auth"
ExtractionOrder("a:1:5;b:1:2")                        →  "b;a"
```

Le plan couvre **tous** les modules : c'est une feuille de route de migration, pas un podium — le
module central que tout le monde appelle y figure, en dernier, et c'est une information : quand son
tour viendra, le système autour de lui se sera vidé et ses entrantes auront fondu.

## Les refus

`ArgumentException` pour une description vide, un module sans ses trois champs, un compte illisible
ou négatif, ou un nom répété.

## Avant d'écrire

Prédisez le plan quand deux modules partagent entrantes et sortantes, puis dites ce que le plan ne
promet pas : les comptes changent à chaque extraction — pourquoi le plan se recalcule-t-il après
chacune au lieu de se suivre aveuglément ?
