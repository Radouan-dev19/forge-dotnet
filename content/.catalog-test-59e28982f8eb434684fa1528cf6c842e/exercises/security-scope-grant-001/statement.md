# Accorder une portée sans jamais l'accorder par défaut

Implémentez `Submission.IsGranted` avec la signature fournie. La fonction décide si un jeton autorise
une opération précise.

## Les deux entrées

`granted` est la liste des portées que porte le jeton, séparées par des espaces — la forme qu'un
serveur d'autorisation utilise réellement. Une entrée préfixée d'un point d'exclamation est un
**refus explicite**.

```text
granted  = "orders:read orders:* !orders:delete"
required = "orders:delete"
```

`required` est l'unique portée exigée par l'opération. Elle est toujours concrète : une portée exigée
qui contiendrait un caractère générique est un défaut d'appel et lève `ArgumentException`, tout comme
une portée exigée vide.

## Les règles de correspondance

Une entrée correspond à la portée exigée dans trois cas seulement :

1. elle est **identique** ;
2. elle se termine par `:*` et la portée exigée commence par le même préfixe suivi de deux points ;
3. elle vaut `*`, qui couvre tout.

La comparaison est **sensible à la casse** : les portées sont des identifiants, pas du texte
d'affichage.

## Les règles de décision

**Un refus explicite l'emporte toujours.** Si une entrée de refus correspond, la réponse est
négative, quelle que soit sa position dans la liste et quelles que soient les autorisations
présentes par ailleurs.

**En l'absence de correspondance, la réponse est négative.** Le moindre privilège n'est pas une
option de configuration : c'est le comportement par défaut. Une liste vide n'accorde donc rien.

Une entrée absente lève `ArgumentNullException`.

## Avant d'écrire

Prédisez quatre cas : une portée accordée nominalement, une portée couverte par un caractère
générique, un refus placé **avant** l'autorisation qui l'accorderait, et une portée exigée qui ne
diffère que par la casse. Nommez ce qui se passerait si la première correspondance rencontrée
décidait.
