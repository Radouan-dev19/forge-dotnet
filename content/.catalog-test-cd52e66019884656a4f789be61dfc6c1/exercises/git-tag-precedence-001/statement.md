# Ordonner deux versions publiées

Implémentez `Submission.CompareVersions` avec la signature fournie.

## Le contrat

```csharp
public static int CompareVersions(string left, string right)
```

Une version porte **exactement trois segments** séparés par des points : majeur, mineur, correctif.
Chaque segment est un entier **positif ou nul**.

Rendez :

| Résultat | Signification |
|---:|---|
| `1` | la version de gauche est la plus récente |
| `-1` | celle de droite l'est |
| `0` | les deux désignent la même version |

## La règle

Chaque segment se compare **comme un nombre**, jamais comme du texte. C'est tout l'enjeu : dans
l'ordre du dictionnaire, `1.2.10` arrive avant `1.2.9`, parce que le caractère `1` précède le
caractère `9`. Une chaîne d'outils qui trie ainsi propose la neuvième correction comme la plus
récente, et déploie une version périmée.

La comparaison s'arrête au **premier écart**. Un majeur supérieur l'emporte quels que soient les
segments suivants : `2.0.0` est plus récente que `1.99.99`. Poursuivre après l'écart laisserait un
segment mineur renverser une décision déjà prise.

## Les refus

`ArgumentNullException` pour une entrée absente. `ArgumentException` pour une version qui ne porte
pas exactement trois segments, ou dont un segment n'est pas un entier positif ou nul — un compte de
segments variable rendrait la comparaison arbitraire, et il vaut mieux le dire que le deviner.

## Avant d'écrire

Prédisez quatre cas : deux correctifs dont l'un dépasse la dizaine, un majeur qui écrase un mineur
élevé, deux versions identiques, une version à deux segments. Nommez ce qu'un tri alphabétique
déploierait en production.
