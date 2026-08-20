# Compter les mutants de borne qui survivent à une suite

Implémentez `Submission.SurvivingMutants` avec la signature fournie. Une règle métier accepte les
entiers d'un intervalle fermé de `low` à `high`. Vos collègues fournissent leur suite de tests sous la
forme des valeurs qu'elle sonde ; votre fonction mesure ce que cette suite vaut vraiment.

## Le principe de la mutation

Un test qui passe ne prouve pas grand-chose tant qu'on ignore ce qu'il aurait détecté. Le test par
mutation retourne la question : on fabrique des variantes fausses de la règle, et on regarde si la
suite en tue au moins une par variante. Ici, les quatre mutants classiques d'un intervalle :

- durcir la borne basse : l'intervalle commence une valeur trop haut ;
- élargir la borne basse : il commence une valeur trop bas ;
- durcir la borne haute : il finit une valeur trop tôt ;
- élargir la borne haute : il finit une valeur trop tard.

Chaque mutant ne diffère de la règle d'origine que sur **une seule valeur d'entrée**. Une sonde tue un
mutant si et seulement si elle vise cette valeur-là. Rendez le nombre de mutants que la suite laisse
survivre.

```text
SurvivingMutants([0, 1, 100, 101], 1, 100)  →  0
SurvivingMutants([50], 1, 100)              →  4
SurvivingMutants([1, 100], 1, 100)          →  2
```

## Les mutants équivalents

Élargir une borne déjà posée sur la plus petite ou la plus grande valeur du type ne produit pas une
règle différente : la valeur qui les distinguerait n'existe pas. Un tel mutant est dit **équivalent**,
et aucun outil sérieux ne le compte comme survivant — sinon le score deviendrait inatteignable sans
que la suite y soit pour rien. Attention au calcul : produire naïvement la valeur d'en dessous de la
limite basse du type déborde et retombe à l'autre extrémité.

## Les refus

Un intervalle dont la borne basse dépasse la borne haute ne définit aucune règle : levez
`ArgumentException` avant tout comptage.

## Avant d'écrire

Prédisez le score de trois suites sur l'intervalle de un à cent : celle qui ne sonde que le milieu,
celle qui ne sonde que les deux bornes, et celle qui sonde les bornes et leurs deux voisines
extérieures. Dites lequel des quatre mutants chacune laisse filer.
