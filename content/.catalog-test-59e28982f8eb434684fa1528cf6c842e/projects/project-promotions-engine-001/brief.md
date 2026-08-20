# Moteur de promotions

Trois règles de remise, un panier, une seule remise appliquée. Toute la difficulté tient dans deux
mots du contrat : **la meilleure**, et **jamais cumulées**.

## Contrat

Le rendu déclare `public static class Submission` et expose exactement ces trois méthodes.

```csharp
public static bool IsEligible(decimal total, int itemCount, bool isMember);
public static decimal BestDiscount(decimal total, int itemCount, bool isMember);
public static string ExplainDecision(decimal total, int itemCount, bool isMember);
```

### Le catalogue

Les règles sont déclarées **dans cet ordre**, qui n'est pas un ordre de priorité mais un ordre de
départage :

| Clé | S'applique quand | Remise |
|---|---|---|
| `volume` | au moins 10 articles | 15 % du total |
| `panier` | total d'au moins 100 | 12,00 fixe |
| `adhesion` | le client est adhérent | 5 % du total |

Une borne est **atteinte donc franchie** : dix articles ouvrent `volume`, un total de cent ouvre
`panier`.

Un total négatif ou un nombre d'articles négatif est un appel fautif et lève
`ArgumentOutOfRangeException`. Un panier à zéro, lui, est parfaitement licite.

### `IsEligible`

Vrai dès qu'au moins une règle s'applique.

### `BestDiscount`

Le montant de la règle **la plus avantageuse**, arrondi à deux décimales, les demis s'éloignant de
zéro. Aucune règle applicable rend zéro.

C'est ici que se joue le piège : une remise proportionnelle bat une remise fixe sur un gros panier et
perd sur un petit. Retenir la première règle applicable ne donne pas le même résultat que retenir la
meilleure.

### `ExplainDecision`

La clé de la règle retenue, une flèche, le montant :

```text
volume -> 30.00
adhesion -> 15.00
aucune -> 0.00
```

Le montant est écrit en culture invariante, toujours avec ses deux décimales. Quand deux règles
donnent **exactement le même montant**, c'est celle qui vient en premier dans le tableau ci-dessus
qui est retenue — sans quoi la même commande donnerait deux explications selon l'ordre d'évaluation.

Aucune règle applicable rend `aucune -> 0.00`, jamais une chaîne vide : un client a droit à une
raison.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les cas visibles vous
montrent leurs échecs ; les cas cachés restent côté serveur. Les trois doivent être vertes pour que
le projet compte comme livrable vérifié.

## Ce qui n'est pas mesuré

Ce que coûterait votre sélection avec deux cents règles au lieu de trois. Sachez répondre : c'est la
question de la semaine.
