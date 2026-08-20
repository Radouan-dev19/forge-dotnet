# Choisir un type monétaire adapté

## Objectif observable

À la fin de cette leçon, vous saurez choisir entre `decimal`, `double` et un entier mis à l'échelle pour représenter un montant, puis justifier ce choix avec les règles d'arrondi et les bornes du domaine.

## Prérequis

- Lire une déclaration de variable C#.
- Connaître la différence entre un nombre entier et un nombre décimal.
- Savoir qu'une opération informatique peut produire un arrondi.

## Intuition

Un montant n'est pas seulement un nombre avec une virgule. Il appartient à un domaine métier qui précise une devise, une précision et un mode d'arrondi. Le bon type rend ces contraintes visibles et limite les surprises.

Imaginez une caisse : une différence d'un centime répétée sur mille lignes devient une erreur comptable. La facilité d'écriture ne suffit donc pas ; il faut préserver les valeurs décimales attendues.

## Explication

`double` représente efficacement des mesures scientifiques approximatives, mais beaucoup de fractions décimales n'ont pas de représentation binaire exacte. `decimal` utilise une représentation adaptée aux calculs décimaux et constitue le choix courant pour les montants métier en .NET.

Un entier exprimé dans la plus petite unité, par exemple des centimes, peut aussi être exact. Il exige toutefois une échelle et une devise explicites pour éviter qu'une valeur `1250` soit interprétée comme 12,50 EUR ou 1 250 EUR.

Le choix doit toujours être accompagné d'une règle d'arrondi au point métier approprié. Arrondir après chaque opération et arrondir uniquement le total final peuvent produire des résultats différents.

## Exemple commenté

Le calcul suivant conserve les valeurs décimales et arrondit une seule fois le total destiné à l'affichage :

```csharp
decimal unitPrice = 19.95m;
int quantity = 3;
decimal taxRate = 0.077m;

decimal subtotal = unitPrice * quantity;
decimal total = decimal.Round(
    subtotal * (1m + taxRate),
    2,
    MidpointRounding.AwayFromZero);
```

Le suffixe `m` rend le type explicite. Le mode d'arrondi est nommé au lieu de dépendre d'une supposition du lecteur.

## Contre-exemple et erreur fréquente

Ce code paraît naturel, mais `double` introduit une approximation binaire et le mode d'arrondi reste implicite :

```csharp
double price = 0.1;
double total = price + price + price;
bool isExpected = total == 0.3;
```

Le test d'égalité peut échouer. Remplacer mécaniquement `double` par `decimal` ne suffit pas non plus : il faut encore définir la précision, le moment de l'arrondi et la devise.

## Vérification de compréhension

Choisissez le type le plus adapté à un total de facture calculé localement, avec deux décimales et une règle d'arrondi métier explicite.

:::quiz
id=money-type-check
question=Quel type C# est le choix courant pour calculer un montant financier décimal ?
option=double, car il est toujours exact pour les fractions décimales
option=decimal, avec une règle d'arrondi métier explicite
option=int, sans documenter l'unité ni l'échelle
correct=1
success=Correct : decimal préserve une arithmétique décimale adaptée, mais la précision et l'arrondi doivent rester explicites.
retry=Relisez la différence entre approximation binaire, arithmétique décimale et entier mis à l'échelle, puis réessayez.
:::

## Exercice guidé

Pour un prix unitaire de 12,40 EUR et une quantité de 4, procédez ainsi :

1. Déclarez le prix en `decimal` et la quantité en `int`.
2. Multipliez les deux valeurs sans convertir vers `double`.
3. Arrondissez le résultat à deux décimales avec un mode nommé.
4. Expliquez pourquoi l'arrondi intervient à cet endroit.

## Exercice autonome

Concevez une fonction qui reçoit un prix hors taxe, un taux de taxe et une quantité. Elle retourne un total à deux décimales. Écrivez vos hypothèses pour les valeurs négatives, les taux supérieurs à 100 % et les quantités nulles avant de proposer le code.

La réponse doit préciser le type de chaque paramètre, le mode d'arrondi et le moment où cet arrondi est appliqué.

## Débogage

Un ticket indique : « Le total affiché diffère parfois d'un centime entre la ligne et la facture. » Reproduisez avec plusieurs lignes, comparez l'arrondi par ligne à l'arrondi final, puis vérifiez le type utilisé à chaque conversion.

- Symptôme : noter les valeurs exactes attendues et obtenues.
- Hypothèse : isoler le moment de l'arrondi.
- Preuve : écrire un test qui échoue avant la correction.
- Prévention : centraliser la politique d'arrondi du domaine.

## Entretien

Question : pourquoi ne pas utiliser systématiquement `double` pour tous les nombres à virgule en C# ?

Une réponse solide distingue performance numérique, représentation binaire, exactitude décimale attendue et règles du domaine. Elle donne un exemple financier et un exemple scientifique où les choix peuvent différer.

## Résumé

- `decimal` est le choix courant pour les montants décimaux métier.
- `double` convient aux mesures approximatives lorsque ses compromis sont acceptés.
- Un entier mis à l'échelle est exact seulement si l'unité est explicite.
- Le type ne remplace jamais une politique d'arrondi documentée et testée.

## Cartes de révision

Question : quel compromis principal distingue `decimal` de `double` ? Réponse attendue : une arithmétique décimale adaptée aux valeurs métier contre une représentation binaire performante adaptée aux calculs approximatifs.

Question : pourquoi nommer le mode d'arrondi ? Réponse attendue : deux conventions valides peuvent donner des résultats différents sur les valeurs médianes.

## Test de maîtrise

Sans relire la leçon, choisissez les types et la politique d'arrondi d'un panier multi-lignes. Justifiez le moment de chaque arrondi, écrivez trois cas limites et expliquez comment votre test détecterait une différence d'un centime.

Ce test est une auto-évaluation de lecture. Il ne crée aucune preuve de maîtrise et n'exécute aucun code dans cet incrément.
