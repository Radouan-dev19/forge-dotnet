# Fonctionnalité livrée sur spécification

Ce projet mesure autre chose que les précédents : votre capacité à livrer une fonctionnalité
complète à partir de sa seule spécification. Le contrat ci-dessous dit **quoi** ; il ne dit jamais
**comment**. Aucun découpage n'est suggéré, aucun harnais n'est fourni, aucun indice n'existe :
c'est volontaire, et c'est ce que le jalon vérifie. Dans les limites du bac à sable — pas de HTTP,
pas de base ici — c'est la définition du geste « fonctionnalité autonome », et elle est déclarée
comme telle.

## La fonctionnalité : relevé de fidélité

Un commerçant crédite des points de fidélité sur les achats de ses clients et veut un relevé
calculé à une date de référence.

**Entrées.** L'historique d'achats est une chaîne d'éléments `aaaa-mm-jj:montant` joints par `;`,
possiblement vide. Les montants ont un point décimal et sont positifs. La date de référence est une
chaîne `aaaa-mm-jj`.

**Fenêtre active.** Un achat est **actif** si son mois civil appartient aux douze mois civils se
terminant au mois de la référence — le mois de la référence compte, le treizième en arrière ne
compte plus. Tout achat hors de cette fenêtre est **expiré**.

**Points de base.** Chaque achat rapporte l'entier inférieur de son montant : `40.99` rapporte 40.

**Bonus mensuel.** Chaque mois civil **actif** dont le total des montants — avant tout arrondi —
atteint `100.00` rapporte 20 points de bonus.

**Niveau.** Sur les points actifs, bonus compris : moins de 100, `bronze` ; moins de 300,
`argent` ; sinon `or`.

## Le contrat

```csharp
public static int    ActivePoints(string purchases, string reference);
public static int    MonthlyBonus(string purchases, string reference);
public static string Statement(string purchases, string reference);
```

`ActivePoints` rend les points de base des achats actifs, sans bonus. `MonthlyBonus` rend le total
des bonus mensuels actifs. `Statement` rend exactement
`actifs=A;expires=E;niveau=N`, où `A` inclut les bonus, `E` compte les points de base des achats
expirés — jamais de bonus sur l'expiré — et `N` est le niveau. Un historique vide rend
`actifs=0;expires=0;niveau=bronze`.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Leurs cas cachés sondent
les bornes du contrat — fenêtre, seuil, niveaux — sans les révéler autrement que par ce texte. Les
trois suites vertes font du projet un livrable vérifié — il satisfait alors l'exigence
**fonctionnalité autonome** de la porte D.

## Ce qui n'est pas mesuré

L'autonomie complète d'un poste de travail réel : négocier la spécification, découper en
livraisons, défendre ses choix. Le projet final et sa grille les observent ; ce projet-ci isole le
geste de livraison sur contrat, dans le périmètre que le bac à sable permet, et le dit.
