# Théories et partitions de cas

## Objectif observable

À la fin de cette leçon, vous saurez découper un domaine d'entrée en partitions, identifier les
frontières où se logent réellement les défauts, et transformer dix tests répétitifs en une théorie
lisible sans perdre le diagnostic d'échec.

## Prérequis

- Avoir lu `tests-domain-rules-001` et savoir isoler une règle testable.
- Avoir lu `csharp-control-methods-001` et savoir lire une condition composée.

## Intuition

On ne peut pas tester toutes les valeurs. Deux idées suffisent à choisir les bonnes.

*La partition* : les entrées se regroupent en classes traitées identiquement. Une valeur par classe
suffit — tester `5`, `6` et `7` quand elles suivent le même chemin n'apporte rien.

*La frontière* : les défauts se concentrent là où le comportement change. Confondre `inférieur` et
`inférieur ou égal` est l'erreur la plus banale de la programmation, et elle ne se voit qu'en testant
la valeur exacte du seuil.

## Explication

**Découper d'abord, choisir ensuite.** Pour une règle acceptant une quantité entre un et mille, les
classes sont : en dessous de un, entre un et mille, au-dessus de mille. Trois classes, donc trois
valeurs représentatives. Ajouter `500` et `700` n'ajoute rien : ils sont dans la même classe.

**Trois valeurs par frontière.** Pour un seuil à cent : quatre-vingt-dix-neuf, cent, cent un. La valeur
juste en dessous, la valeur exacte, la valeur juste au-dessus. Ces trois-là attrapent l'erreur de
comparaison stricte, qui est la plus fréquente de toutes.

Pour un intervalle fermé, il y a deux frontières, donc six valeurs — et c'est ce qui explique qu'une
règle en apparence triviale mérite six tests.

**Les valeurs particulières comptent comme des classes.** Zéro, le négatif, la valeur absente, la
chaîne vide, la chaîne de blancs, la collection vide, la valeur maximale du type. Chacune est une
classe à part entière, parce que chacune emprunte souvent un chemin distinct — ou révèle qu'aucun
chemin n'a été prévu.

**Une théorie factorise les cas, pas le raisonnement.** Un test paramétré exécute le même corps avec
plusieurs jeux de données. C'est le bon outil quand les cas diffèrent seulement par leurs valeurs.
Chaque jeu apparaît séparément dans le rapport : un échec nomme la valeur fautive, ce qui préserve le
diagnostic.

Le piège est d'y mettre des cas qui ne partagent pas la même logique. Si le corps doit contenir une
condition sur les données d'entrée pour savoir quoi vérifier, il y a deux tests, pas une théorie.

**Un jeu de données doit rester lisible.** Une ligne de données avec sept paramètres, dont trois
booléens, est illisible dans un rapport d'échec. Nommer les cas ou passer par une source de données
typée vaut mieux qu'une longue suite de littéraux dont on ne sait plus lequel est lequel.

**Les théories ne dispensent pas de nommer.** Le nom du test énonce toujours la règle : la théorie
énonce la règle générale, et le jeu de données fournit les instances. Un nom vague dans une théorie
est pire que dans un test simple, parce qu'il couvre plus de cas.

**Ce que les frontières ne couvrent pas.** Les combinaisons. Deux paramètres à trois classes chacun
font neuf combinaisons, et toutes ne sont pas nécessaires. La pratique consiste à couvrir chaque classe
au moins une fois, puis à ajouter explicitement les combinaisons dont on sait qu'elles interagissent —
comme un seuil de gratuité couplé à un mode de livraison.

## Exemple commenté

Une frontière, réduite à sa question :

```csharp
public static bool IsBoundary(int value, int minimum, int maximum)
{
    // Des bornes incohérentes ne définissent aucun intervalle : c'est une faute
    // d'appelant, pas un cas à traiter silencieusement.
    ArgumentOutOfRangeException.ThrowIfGreaterThan(minimum, maximum);

    // Exactement l'une des deux extrémités, comparées à l'identique.
    return value == minimum || value == maximum;
}
```

La théorie qui couvre les six valeurs d'un intervalle fermé :

```csharp
[Theory]
[InlineData(0, false)]       // juste en dessous du minimum
[InlineData(1, true)]        // le minimum exact
[InlineData(2, true)]        // juste au-dessus du minimum
[InlineData(999, true)]      // juste en dessous du maximum
[InlineData(1_000, true)]    // le maximum exact
[InlineData(1_001, false)]   // juste au-dessus du maximum
public void Quantite_SelonLaValeur_EstAcceptéeOuRefusee(int quantity, bool expected) =>
    // Un seul comportement, plusieurs instances. Chaque jeu apparaît séparément
    // dans le rapport : un échec nomme la valeur fautive.
    Assert.Equal(expected, OrderRules.IsValidQuantity(quantity));
```

Et une théorie sur deux dimensions, où la combinaison est choisie explicitement :

```csharp
[Theory]
[InlineData(49.99, false, 4.9)]    // sous le seuil, livraison normale
[InlineData(50.00, false, 0.0)]    // le seuil exact : la gratuité s'applique
[InlineData(50.00, true, 9.9)]     // le seuil exact, mais l'express reste payant
[InlineData(120.00, true, 9.9)]    // bien au-dessus : l'express ne devient pas gratuit
public void FraisDePort_SelonMontantEtMode_SontCalcules(
    decimal total, bool express, decimal expected) =>
    // Les quatre lignes ne sont pas le produit cartésien : ce sont les combinaisons
    // où les deux critères interagissent réellement.
    Assert.Equal(expected, Shipping.Cost(total, express));
```

## Contre-exemple et erreur fréquente

```csharp
[Theory]
[InlineData(5)]
[InlineData(6)]
[InlineData(7)]
[InlineData(8)]
[InlineData(9)]
public void Quantite_EstValide(int quantity) =>
    // Cinq valeurs de la même classe : cinq fois le même chemin, aucune frontière touchée.
    Assert.True(OrderRules.IsValidQuantity(quantity));

[Theory]
[InlineData(10, true, "A", 3, false, 1)]
[InlineData(20, false, "B", 0, true, 2)]
public void Calcul_EstCorrect(int a, bool b, string c, int d, bool e, int expected)
{
    // Le corps décide quoi vérifier selon les données : ce ne sont pas des instances
    // d'une même règle, ce sont deux tests différents entassés dans une théorie.
    if (b)
    {
        Assert.Equal(expected, Service.Calculer(a, c));
    }
    else
    {
        Assert.Equal(expected, Service.CalculerAutrement(a, d, e));
    }
}
```

Trois défauts.

La première théorie multiplie les cas sans rien couvrir de nouveau : les cinq valeurs appartiennent à
la même classe. Zéro, un, mille et mille un auraient trouvé des défauts ; cinq à neuf n'en trouveront
jamais.

La seconde entasse deux règles distinctes. La condition dans le corps est le signe : quand il faut
regarder les données pour savoir quoi vérifier, ce sont deux tests. En prime, un échec sur la ligne
deux n'indique pas quelle branche a échoué.

Enfin, six paramètres dont deux booléens sans nom rendent le rapport d'échec illisible :
`Calcul_EstCorrect(a: 20, b: False, c: "B", d: 0, e: True, expected: 2)` n'apprend rien à qui n'a pas
le code sous les yeux.

## Vérification de compréhension

Pour une règle acceptant un âge entre dix-huit et soixante-sept ans inclus, listez les valeurs que
vous testez et justifiez chacune par la classe ou la frontière qu'elle couvre.

:::quiz
id=tests-boundaries-theories-001-check
question=Pourquoi tester trois valeurs autour d'un seuil plutôt que la seule valeur du seuil ?
option=Parce que le cadre de test exige au moins trois jeux de données par théorie
option=Parce que les trois valeurs distinguent une comparaison stricte d'une comparaison large : l'erreur la plus fréquente ne se voit que là
option=Parce qu'une valeur unique ne peut pas être passée en paramètre d'une théorie
correct=1
success=Correct : juste en dessous, exactement, juste au-dessus — c'est ce triplet qui attrape la confusion entre « inférieur » et « inférieur ou égal ».
retry=Relisez le passage sur les frontières, et demandez-vous quelle erreur passe inaperçue si l'on ne teste que la valeur exacte.
:::

## Exercice guidé

Ouvrez `tests-boundary-values-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire un appel dont les bornes sont incohérentes.
2. Implémentez la détection en refusant explicitement ce cas.
3. Vérifiez les deux extrémités, une valeur intérieure et une valeur extérieure.
4. Enchaînez avec `tests-shipping-theory-001`, qui fait varier deux critères dans une même théorie.

## Exercice autonome

Concevez le jeu de tests d'une règle de tarification par tranches : trois paliers, un plafond, un cas
de gratuité.

Décidez avant d'écrire : les partitions, les frontières exactes, les valeurs particulières, ce qui
tient dans une théorie et ce qui mérite un test distinct, et la façon dont vous nommez chaque jeu de
données pour qu'un échec soit lisible.

## Débogage

Un ticket indique : « Le client à exactement mille euros n'a pas eu la remise annoncée. »

1. **Symptôme** : le défaut apparaît sur une valeur unique, celle du seuil.
2. **Hypothèse** : la comparaison est stricte là où elle devait être large.
3. **Preuve** : appelez la règle avec la valeur juste en dessous, la valeur exacte et la valeur juste
   au-dessus. Un résultat inattendu sur la valeur exacte confirme.
4. **Prévention** : ajouter le triplet de frontière à la théorie, et exiger ce triplet pour tout seuil
   introduit par la suite.

## Entretien

Question posée à voix haute : *comment choisissez-vous les valeurs de vos tests ?*

Une réponse solide part des partitions, ajoute les frontières et les valeurs particulières, sait dire
pourquoi cinq valeurs d'une même classe n'apportent rien, et connaît la limite des théories — elles
factorisent des instances, pas des règles différentes.

## Résumé

- Une valeur par classe d'équivalence, pas cinq.
- Trois valeurs par frontière : en dessous, exactement, au-dessus.
- Zéro, négatif, vide et absent sont des classes à part entière.
- Une théorie factorise des instances d'une même règle, jamais deux règles.
- Un jeu de données illisible rend le rapport d'échec inutile.

## Cartes de révision

Question : combien de valeurs pour couvrir correctement un intervalle fermé ? Réponse attendue : six,
trois par frontière.

Question : quel signe montre qu'une théorie contient en fait deux tests ? Réponse attendue : son corps
comporte une condition sur les données d'entrée.

## Test de maîtrise

Sans relire, concevez le jeu de tests d'une règle d'éligibilité combinant un âge, une ancienneté et un
statut : partitions de chaque paramètre, frontières exactes, valeurs particulières, combinaisons
retenues et justification de celles qui sont écartées, découpage entre théories et tests simples.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
