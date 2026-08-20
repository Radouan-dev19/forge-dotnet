# Interfaces ciblées et composition

## Objectif observable

À la fin de cette leçon, vous saurez découper une responsabilité en interfaces d'une ou deux méthodes,
assembler un service par injection de dépendances plutôt que par héritage, et écrire un test qui
substitue une seule de ces dépendances.

## Prérequis

- Avoir lu `oop-encapsulation-001` et savoir protéger un invariant.
- Savoir déclarer une interface et l'implémenter dans une classe.

## Intuition

Une interface décrit une capacité observable : *arrondir un montant*, *fournir l'heure*, *envoyer une
notification*. Plus elle est petite, plus elle est facile à implémenter, à substituer et à réutiliser.

L'héritage, lui, impose une hiérarchie : la sous-classe hérite de tout, y compris de ce dont elle n'a
pas besoin. La composition assemble des capacités indépendantes, ce qui permet de changer l'une sans
toucher aux autres.

## Explication

**Une interface se dimensionne par son consommateur.** Le critère utile n'est pas « quelles méthodes
cette classe possède-t-elle ? » mais « de quoi mon appelant a-t-il strictement besoin ? ». Un
calculateur de remise qui n'a besoin que d'arrondir ne doit pas dépendre d'une interface
`IMathServices` de quinze méthodes : il dépend d'un `IRounder` d'une seule. Le bénéfice est concret —
un test n'a plus qu'une méthode à fournir au lieu de quinze levées `NotImplementedException`.

**Dépendre d'une abstraction pour ce qui varie ou ce qui gêne.** Toute dépendance n'a pas besoin
d'être abstraite : introduire une interface pour une classe qui n'aura jamais qu'une implémentation
ajoute de l'indirection sans bénéfice. Les candidats légitimes sont ce qui varie selon le contexte
(une politique de remise, un format d'export) et ce qui rend un test impossible ou fragile (l'horloge,
le système de fichiers, le réseau).

L'horloge est l'exemple canonique. `DateTime.Now` en dur rend une règle d'expiration impossible à
tester sans attendre. .NET fournit `TimeProvider` précisément pour cela : le service reçoit son
horloge, le test lui en fournit une déterministe.

**La composition remplace la hiérarchie.** Le réflexe « j'ai deux comportements proches, je crée une
classe de base » produit rapidement une classe qui sait tout et dont les sous-classes n'utilisent
qu'un tiers. La question à poser avant d'hériter : *est-ce que la sous-classe est réellement
substituable à la base partout où celle-ci est attendue ?* Si la réponse exige un « sauf que », la
composition est le bon outil.

En pratique, la classe reçoit ses collaborateurs par constructeur, les garde en champs `readonly`, et
délègue. Chaque collaborateur peut être remplacé indépendamment, et le constructeur documente
exactement de quoi la classe a besoin pour fonctionner.

**Le constructeur est la liste des dépendances.** Une classe dont le constructeur prend six
interfaces annonce qu'elle fait six choses ; c'est un signal de découpage bien plus fiable que le
nombre de lignes. À l'inverse, une classe qui crée ses collaborateurs elle-même avec `new` cache ses
dépendances et interdit toute substitution — c'est ce que l'injection vient corriger.

**Le test devient la preuve du découpage.** Si substituer une seule dépendance demande de construire
la moitié de l'application, le découpage est raté. Si un test peut fournir une horloge fixe et une
politique de remise triviale en trois lignes, il est réussi. Le test n'est pas seulement une
vérification : c'est le premier consommateur qui met le découpage à l'épreuve.

## Exemple commenté

```csharp
// Deux capacités indépendantes, une méthode chacune.
public interface IDiscountPolicy
{
    decimal RateFor(int quantity);
}

public interface IRounder
{
    decimal Round(decimal amount);
}

public sealed class PriceCalculator
{
    private readonly IDiscountPolicy _policy;
    private readonly IRounder _rounder;

    // Le constructeur énumère exactement ce dont la classe a besoin.
    public PriceCalculator(IDiscountPolicy policy, IRounder rounder)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(rounder);
        _policy = policy;
        _rounder = rounder;
    }

    public decimal ComputeTotal(decimal unitPrice, int quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        decimal gross = unitPrice * quantity;
        // La règle de remise est déléguée : la changer ne touche pas ce calcul.
        return _rounder.Round(gross * (1m - _policy.RateFor(quantity)));
    }
}
```

Un test peut substituer la seule politique de remise, en gardant l'arrondi réel :

```csharp
var calculator = new PriceCalculator(
    policy: new FixedRatePolicy(0.10m),   // remise fixe, triviale à écrire
    rounder: new BankersRounder());       // implémentation réelle conservée

Assert.Equal(90m, calculator.ComputeTotal(unitPrice: 10m, quantity: 10));
```

Aucune infrastructure n'est nécessaire, et le test échouerait si la remise cessait d'être appliquée.

## Contre-exemple et erreur fréquente

```csharp
public abstract class CalculatorBase
{
    protected decimal Round(decimal amount) => Math.Round(amount, 2);
    protected decimal RateFor(int quantity) => quantity >= 10 ? 0.10m : 0m;
    protected DateTime Now => DateTime.Now;          // Horloge en dur, non substituable.
    protected virtual string Format(decimal v) => v.ToString("C");
}

public sealed class PriceCalculator : CalculatorBase
{
    public decimal ComputeTotal(decimal unitPrice, int quantity)
        => Round(unitPrice * quantity * (1m - RateFor(quantity)));
}
```

La classe de base rassemble quatre responsabilités sans rapport entre elles. Changer la règle de
remise oblige à toucher un type dont héritent toutes les autres calculatrices, ou à ajouter une
surcharge `virtual` de plus. Le formatage est hérité alors que `PriceCalculator` ne s'en sert pas.

Le défaut décisif est `Now` : toute règle dépendant de la date devient intestable sans manipuler
l'horloge de la machine. Un test « qui passe le mardi » n'est pas un test.

La correction consiste à supprimer l'héritage et à injecter trois collaborateurs indépendants — la
politique, l'arrondisseur, et un `TimeProvider` pour l'horloge.

## Vérification de compréhension

Pour un service qui envoie un rappel d'échéance, nommez les dépendances à injecter et, pour chacune,
la raison : variabilité métier ou testabilité.

:::quiz
id=oop-interfaces-composition-001-check
question=Quel signal indique qu'une interface est trop large ?
option=Elle est implémentée par plus d'une classe dans l'application
option=Un test doit fournir des méthodes dont le cas testé n'a aucun besoin
option=Son nom ne commence pas par la lettre I
correct=1
success=Correct : une interface se dimensionne par son consommateur, et les implémentations vides d'un test révèlent précisément ce qui dépasse ce besoin.
retry=Relisez le passage sur le dimensionnement par le consommateur, et l'exemple du calculateur qui n'a besoin que d'arrondir.
:::

## Exercice guidé

Ouvrez `csharp-payment-fee-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la liste des collaborateurs nécessaires avant tout code, avec la raison de chacun.
2. Déclarez chaque interface avec le minimum de méthodes utiles au consommateur.
3. Implémentez le service, en gardant les collaborateurs en champs `readonly`.
4. Écrivez un test qui substitue **une seule** dépendance et vérifiez qu'il tient en quelques lignes.

## Exercice autonome

Concevez un service qui décide si une commande peut être annulée : possible tant qu'elle n'est pas
expédiée et que moins de vingt-quatre heures se sont écoulées.

Décidez avant de coder : quelles dépendances injecter, lesquelles laisser concrètes, et comment votre
test fixera l'heure sans dépendre de la machine. Justifiez chaque abstraction introduite.

## Débogage

Un ticket indique : « Le test d'expiration échoue une fois par mois, toujours le premier du mois. »

1. **Symptôme** : l'échec est intermittent et corrélé au calendrier.
2. **Hypothèse** : le code sous test lit l'horloge système au lieu de recevoir une date.
3. **Preuve** : cherchez les appels directs à l'horloge dans le chemin exécuté. Un seul suffit à
   expliquer le symptôme.
4. **Prévention** : injectez un `TimeProvider`, et ajoutez un test qui fixe explicitement la date au
   dernier jour d'un mois de 31 jours.

## Entretien

Question posée à voix haute : *héritage ou composition — comment tranchez-vous ?*

Une réponse solide donne un critère de substituabilité plutôt qu'une préférence de style, reconnaît
les cas où l'héritage reste adapté, et cite une situation vécue où une classe de base est devenue un
point de couplage difficile à défaire.

## Résumé

- Une interface se dimensionne par le besoin de son consommateur, pas par les capacités de la classe.
- Abstraire ce qui varie ou ce qui rend un test impossible ; laisser le reste concret.
- Le constructeur énumère les dépendances et sert de mesure du découpage.
- L'horloge, le disque et le réseau se reçoivent, ne se créent pas.
- Un test qui exige la moitié de l'application révèle un découpage raté.

## Cartes de révision

Question : quelle question poser avant de créer une classe de base commune ? Réponse attendue : la
sous-classe est-elle réellement substituable à la base partout où celle-ci est attendue ?

Question : pourquoi injecter l'horloge plutôt que d'appeler `DateTime.Now` ? Réponse attendue : sans
cela, toute règle dépendant du temps devient intestable de façon déterministe.

## Test de maîtrise

Sans relire, découpez un service d'envoi de facture en interfaces d'une ou deux méthodes. Justifiez
chaque abstraction par la variabilité ou la testabilité, écrivez le constructeur, et décrivez le test
qui substitue une seule dépendance pour vérifier qu'une facture soldée n'est jamais envoyée.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
