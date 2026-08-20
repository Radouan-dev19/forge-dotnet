# Produire les sondes qui réfutent un décalage de borne

Implémentez `Submission.BoundaryProbes` avec la signature fournie. Une règle métier accepte les
entiers d'un intervalle fermé ; votre fonction rend les valeurs à tester pour prouver que ses deux
bornes sont exactes.

## Ce qu'il faut produire

Une erreur d'une unité sur une borne est la faute la plus fréquente et la plus discrète : le code
compile, la règle fonctionne au milieu de l'intervalle, et seule la frontière est fausse. La réfuter
demande quatre valeurs et pas une de plus :

- la première valeur **hors** de l'intervalle du côté bas ;
- la première valeur **dans** l'intervalle du côté bas ;
- la dernière valeur **dans** l'intervalle du côté haut ;
- la première valeur **hors** de l'intervalle du côté haut.

Le résultat est trié par ordre croissant et sans doublon. Deux bornes confondues produisent donc
trois sondes, pas quatre.

```text
BoundaryProbes(1, 100)  →  [0, 1, 100, 101]
BoundaryProbes(5, 5)    →  [4, 5, 6]
```

## Les limites du type

Une borne posée sur la limite du type est elle-même une frontière : **il n'existe aucune valeur
au-delà**. La sonde extérieure correspondante est alors absente du résultat, et ce n'est pas un
oubli. Un intervalle qui couvre tout le type ne produit donc que ses deux bornes.

## Les refus

Un intervalle dont la borne basse dépasse la borne haute est vide : il n'a aucune frontière à sonder
et lève `ArgumentException`.

## Avant d'écrire

Prédisez quatre cas : un intervalle large, un intervalle d'une seule valeur, un intervalle de deux
valeurs contiguës, et un intervalle appuyé sur une limite du type. Nommez l'erreur que chacune des
quatre sondes réfute, et dites laquelle manquerait si vous ne testiez que l'intérieur.
