# Tester les règles du domaine

## Objectif observable

À la fin de cette leçon, vous saurez distinguer un test qui vérifie une règle métier d'un test qui
vérifie une implémentation, écrire une règle testable sans base de données ni horloge, et expliquer
pourquoi le second type de test se casse à chaque refactoring.

## Prérequis

- Avoir lu `tests-xunit-aaa-001` et savoir structurer un test.
- Avoir lu `oop-encapsulation-001` et savoir protéger un invariant.

## Intuition

Deux tests peuvent porter sur le même code et n'avoir pas du tout la même valeur.

Le premier dit : *une commande de plus de mille euros bénéficie de la livraison gratuite*. Il survivra
à toutes les réécritures internes, parce qu'il énonce ce que le métier attend.

Le second dit : *la méthode `CalculerFrais` appelle `ObtenirTarif` puis multiplie par le poids*. Il
casse dès qu'on réorganise le code, sans qu'aucun comportement n'ait changé. Il ne protège rien : il
fige une implémentation.

## Explication

**Le test de règle porte sur l'observable.** Entrée, sortie. Il ne connaît ni les méthodes privées, ni
l'ordre des appels internes, ni la structure des classes. C'est ce qui lui permet de rester vrai après
une réécriture — et c'est exactement le filet dont parle `quality-regression-refactoring-001`.

Le signe qu'on a franchi la ligne : le test échoue alors qu'aucun comportement observable n'a changé.
Ce test coûte à chaque modification et ne détecte aucune régression réelle.

**Une règle du domaine ne parle à personne d'autre.** Pas de base de données, pas de réseau, pas
d'heure courante, pas de configuration. Elle prend ce dont elle a besoin en paramètre et retourne un
résultat. Cette pureté n'est pas une exigence esthétique : c'est ce qui rend le test instantané,
déterministe et lisible.

Quand une règle a besoin du temps, il lui est **fourni**. Quand elle a besoin d'un taux, il lui est
fourni. La lecture de l'horloge et le chargement du taux appartiennent à la couche qui appelle la
règle, pas à la règle.

**Le domaine se teste par les frontières et par les cas interdits.** Une règle correcte au milieu de
son domaine et fausse à ses bords est une règle fausse. Et une règle qui accepte silencieusement une
entrée absurde reporte le défaut plus loin, là où sa cause sera introuvable.

Tester le refus est aussi important que tester l'acceptation : `Assert.Throws` sur une entrée
impossible documente le contrat, exactement comme le fait une contrainte de base dans
`sql-relational-constraints-001`.

**Les invariants se testent depuis l'extérieur.** Si une classe garantit qu'un panier ne peut pas
avoir de total négatif, le test essaie de le rendre négatif par les moyens publics et vérifie le
refus. Rendre un membre visible pour le tester revient à tester ce que personne n'utilise, et affaiblit
l'encapsulation au passage.

**Le nombre de tests suit le nombre de comportements, pas de lignes.** Une règle avec quatre branches
et trois frontières demande sept tests, qu'elle tienne en cinq lignes ou en cinquante. Le taux de
couverture ne dit rien de cela : cent pour cent de lignes couvertes avec une seule assertion triviale
est courant, et sans valeur.

**Une règle mal placée est difficile à tester.** Si écrire le test demande de démarrer une application
ou d'ouvrir une base, la règle n'est pas dans le domaine — elle est dans le contrôleur ou dans le
dépôt. La difficulté du test est un diagnostic de conception, pas un problème de test.

## Exemple commenté

Une règle pure, avec tout ce dont elle a besoin en paramètre :

```csharp
public static class DiscountRules
{
    // Aucune lecture externe : ni horloge, ni configuration, ni base.
    // Le résultat ne dépend que des arguments, donc le test est déterministe.
    public static decimal DiscountRate(decimal orderTotal)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(orderTotal);

        if (orderTotal >= 200m)
        {
            return 0.10m;
        }

        return orderTotal >= 100m ? 0.05m : 0m;
    }
}
```

Les tests correspondants : une partition et ses frontières exactes.

```csharp
[Fact]
public void Remise_SousLePremierPalier_EstNulle() =>
    Assert.Equal(0m, DiscountRules.DiscountRate(99.99m));

[Fact]
public void Remise_ExactementAuPremierPalier_EstDeCinqPourCent() =>
    // La frontière exacte : c'est là que se cachent les erreurs de comparaison stricte.
    Assert.Equal(0.05m, DiscountRules.DiscountRate(100m));

[Fact]
public void Remise_ExactementAuSecondPalier_EstDeDixPourCent() =>
    Assert.Equal(0.10m, DiscountRules.DiscountRate(200m));

[Fact]
public void Remise_MontantNegatif_EstRefusee() =>
    // Le refus fait partie du contrat : il se teste comme le reste.
    Assert.Throws<ArgumentOutOfRangeException>(() => DiscountRules.DiscountRate(-1m));
```

Et l'invariant vérifié par les seuls moyens publics :

```csharp
[Fact]
public void Panier_RetirerPlusQueLaQuantitePresente_EstRefuse()
{
    Cart cart = new();
    cart.Add(new Item("ref-1", 2));

    // On tente de violer l'invariant par l'interface publique, et on exige le refus.
    // Rendre un champ visible pour l'inspecter testerait ce que personne n'utilise.
    Assert.Throws<InvalidOperationException>(() => cart.Remove("ref-1", 3));
    Assert.Equal(2, cart.QuantityOf("ref-1"));
}
```

## Contre-exemple et erreur fréquente

```csharp
[Fact]
public void CalculerFrais_AppelleObtenirTarifPuisMultiplie()
{
    var tarifs = new Mock<ITarifs>();
    tarifs.Setup(t => t.Obtenir("FR")).Returns(3m);
    var calculateur = new CalculateurFrais(tarifs.Object);

    decimal frais = calculateur.Calculer("FR", poids: 2);

    // On vérifie l'ordre et le nombre d'appels internes, pas le résultat métier.
    tarifs.Verify(t => t.Obtenir("FR"), Times.Once);
    Assert.Equal(6m, frais);
}

[Fact]
public void RegleDeLivraison_EstCorrecte()
{
    // La règle lit l'horloge et la base : le test doit monter toute l'infrastructure,
    // devient lent, et échouera un jour férié ou après un changement de données.
    using var context = new AppDbContext(RealConnectionString);
    var service = new ShippingService(context);

    Assert.True(service.EstGratuite(commandeId: 42));
}
```

Trois défauts de nature différente.

La vérification `Times.Once` fige l'implémentation. Mettre le tarif en cache — un changement sans
effet observable — fera échouer ce test. Il coûte à chaque refactoring et ne protège aucun
comportement.

Le second test dépend d'une base réelle et de la commande numéro 42. Il est lent, il échoue si la
donnée change, et son échec n'apprend pas si la règle est fausse ou si le jeu de données a bougé.

Son nom, enfin, n'énonce aucune règle. `EstCorrecte` ne dit ni la condition, ni le résultat attendu :
en cas d'échec, il faut lire le code.

La correction : extraire la règle en fonction pure recevant le montant et la destination, la tester
directement sur ses frontières, et laisser au test d'intégration la seule question qu'il traite bien —
la donnée est-elle correctement chargée.

## Vérification de compréhension

Vous remplacez une boucle par une expression équivalente, sans changer aucun comportement observable,
et trois tests échouent. Dites ce que cela révèle sur ces trois tests.

:::quiz
id=tests-domain-rules-001-check
question=Quel signe indique qu'un test vérifie une implémentation plutôt qu'une règle ?
option=Il utilise des valeurs littérales dans ses assertions
option=Il échoue après une réécriture interne alors qu'aucun comportement observable n'a changé
option=Il s'exécute plus lentement que les autres tests de la suite
correct=1
success=Correct : un tel test fige une structure interne. Il coûte à chaque modification sans détecter la moindre régression réelle.
retry=Relisez le passage sur la ligne entre règle et implémentation, et demandez-vous ce qui doit se passer lors d'un refactoring.
:::

## Exercice guidé

Ouvrez `tests-discount-rule-001` dans `/practice`, puis procédez ainsi.

1. Listez, avant tout code, les partitions de montant et les deux frontières exactes.
2. Implémentez la règle sans lire quoi que ce soit d'extérieur à ses paramètres.
3. Vérifiez chaque partition et chaque frontière séparément, y compris le cas refusé.
4. Enchaînez avec `tests-expiry-clock-001`, qui isole la même règle du temps.

## Exercice autonome

Écrivez la règle et les tests d'un calcul de pénalité de retard : montant dû, date d'échéance, date
observée, taux journalier plafonné.

Décidez avant d'écrire : la signature de la règle, ce qui lui est fourni plutôt que lu, la liste des
comportements, les frontières exactes, les entrées refusées, et le test qui prouve que le plafond
tient.

## Débogage

Un ticket indique : « Après un nettoyage de code sans changement fonctionnel, la moitié des tests
échouent. »

1. **Symptôme** : des échecs massifs sans modification de comportement.
2. **Hypothèse** : les tests vérifient des appels internes plutôt que des résultats.
3. **Preuve** : lisez les assertions. Une majorité de vérifications d'appels et de membres internes
   confirme.
4. **Prévention** : réécrire ces tests sur l'observable, et refuser en revue toute assertion sur
   l'ordre des appels quand le résultat suffit.

## Entretien

Question posée à voix haute : *que testez-vous en priorité dans une application ?*

Une réponse solide place les règles du domaine en premier, justifie par leur pureté et leur
stabilité, distingue explicitement test de règle et test d'implémentation, et sait dire que la
difficulté à écrire un test révèle un problème de conception.

## Résumé

- Un test de règle porte sur l'observable et survit aux réécritures.
- Une règle du domaine ne lit ni horloge, ni base, ni configuration.
- Frontières et cas refusés font partie du contrat, donc des tests.
- Les invariants se vérifient par l'interface publique, sans ouvrir la classe.
- Un test difficile à écrire signale une règle mal placée.

## Cartes de révision

Question : pourquoi le taux de couverture ne mesure-t-il pas la qualité d'une suite ? Réponse
attendue : une ligne exécutée sans assertion utile compte comme couverte.

Question : à qui appartient la lecture de l'horloge si la règle en a besoin ? Réponse attendue : à la
couche qui appelle la règle, jamais à la règle elle-même.

## Test de maîtrise

Sans relire, écrivez la règle et les tests d'un calcul de commission : signature, ce qui est fourni
plutôt que lu, partitions, frontières exactes, entrées refusées, invariant garanti, et la
justification de chaque test par le comportement qu'il protège.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
