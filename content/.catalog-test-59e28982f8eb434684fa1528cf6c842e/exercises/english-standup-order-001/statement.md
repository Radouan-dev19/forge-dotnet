# Remettre un point quotidien dans l'ordre

Implémentez `Submission.OrderStandup` avec la signature fournie.

Un point quotidien tient en trois parties, toujours les mêmes et toujours dans le même ordre : ce qui
est **fait**, ce qui vient **ensuite**, ce qui **bloque**. Cet ordre n'est pas une convention
gratuite : le blocage arrive en dernier parce que c'est la seule partie qui appelle une réponse, et
on ne demande pas d'aide avant d'avoir dit où l'on en est.

## Le format reçu

Des lignes séparées par un saut de ligne, chacune préfixée d'une étiquette et d'un deux-points :

```text
next: review the pagination PR
done: fixed the CSV import
blocker: waiting for staging credentials
```

Les trois étiquettes attendues sont `done`, `next` et `blocker`. La comparaison **ignore la casse**
et les blancs autour de l'étiquette comme du texte.

## La règle

Rendez les lignes dans l'ordre canonique `done`, `next`, `blocker`, quel que soit leur ordre
d'écriture. Deux lignes portant la **même** étiquette conservent entre elles leur ordre d'arrivée :
c'est une chronologie, pas un classement.

Chaque ligne rendue prend la forme `<étiquette en minuscules>: <texte>`, jointes par un saut de ligne.

Une ligne dont l'étiquette n'est pas l'une des trois est **écartée**. Une ligne sans texte utile
aussi : une étiquette seule occupe une place sans rien dire.

Une entrée absente lève `ArgumentNullException`. Une entrée sans aucune ligne exploitable rend une
chaîne vide.

## Avant d'écrire

Prédisez quatre cas : les trois parties écrites à l'envers, deux lignes de même étiquette, une
étiquette inventée, une étiquette sans texte. Nommez ce que l'équipe perd quand le blocage est
annoncé en premier.
