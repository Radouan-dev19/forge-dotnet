# Intégration et base de test isolée

## Objectif observable

À la fin de cette leçon, vous saurez décider ce qui mérite un test d'intégration, garantir qu'une base
de test est jetable et propre à son exécution, et reconnaître les deux pratiques qui rendent une suite
d'intégration inexploitable.

## Prérequis

- Avoir lu `tests-doubles-design-001` et savoir substituer une dépendance.
- Avoir lu `ef-core-migrations-001` et savoir créer un schéma depuis le code.

## Intuition

Un test d'intégration ne répond pas à la même question qu'un test unitaire. Le test unitaire demande
*« la règle est-elle juste ? »*. Le test d'intégration demande *« le morceau que je n'ai pas écrit
se comporte-t-il comme je le crois ? »* — la correspondance objet-relationnel, la contrainte
d'unicité, la conversion de type, le comportement d'une transaction.

Il est plus lent, plus fragile, plus coûteux. Il faut donc le réserver à ce que rien d'autre ne peut
prouver.

## Explication

**Ce qui mérite un test d'intégration.** La correspondance entre le modèle et le schéma. Les
contraintes qui vivent en base : unicité, clé étrangère, non-nullité. Les requêtes non triviales — les
jointures et agrégats de `sql-joins-001`. Le comportement transactionnel. Tout ce qui, en pratique,
n'existe pas tant qu'une vraie base ne l'exécute pas.

**Ce qui n'en mérite pas.** Une règle du domaine. Une conversion. Un calcul. Les tester en passant par
la base rend la suite lente sans rien prouver de plus — et rend l'échec ambigu : la règle est-elle
fausse, ou la donnée a-t-elle changé ?

**L'isolation est la condition de tout le reste.** Chaque exécution travaille sur sa propre base,
créée puis détruite. Un nom unique par exécution — un préfixe réservé et un suffixe suffisamment
aléatoire — permet à deux exécutions parallèles de coexister sans se marcher dessus, ce qui devient
indispensable dès que la chaîne de construction lance plusieurs travaux.

Le préfixe réservé sert d'autre chose : il rend le nettoyage sûr. Un script qui supprime les bases
préfixées ne pourra jamais supprimer une base réelle par accident.

**Ne jamais viser une base réelle.** C'est la règle qui n'a pas d'exception. Un test qui pointe une
base de recette peut la vider, la corrompre ou en publier le contenu dans un rapport d'échec. Dans ce
dépôt, le laboratoire SQL provisionne toujours une base jetable, et la progression de l'apprenant vit
ailleurs.

**Le double emploi d'une base en mémoire.** Une base en mémoire est rapide et pratique, mais elle ne
respecte ni les contraintes d'unicité relationnelles, ni les types, ni les transactions du moteur
réel. Un test qui vérifie une contrainte contre une base en mémoire ne vérifie rien. Le fournisseur
utilisé en test doit être celui de production, sinon le test valide une fiction — c'est le risque du
fake qui ment, vu dans `tests-doubles-design-001`.

**Chaque test part d'un état connu.** Deux stratégies. La première recrée le schéma et les données de
départ avant chaque test : la plus lente, la plus sûre. La seconde ouvre une transaction, exécute le
test, puis annule : très rapide, mais inutilisable si le code sous test gère lui-même ses
transactions.

Ce qu'il ne faut pas, c'est dépendre de l'état laissé par le test précédent. C'est le même défaut
d'état partagé que dans `tests-xunit-aaa-001`, avec des conséquences plus difficiles à diagnostiquer.

**Vérifier depuis l'extérieur du code testé.** Après une écriture, relire par une nouvelle connexion
ou un nouveau contexte. Vérifier depuis le contexte qui a écrit peut lire un objet encore en mémoire,
et le test passera même si rien n'a atteint la base. Un identifiant strictement positif attribué par
la base est le signe le plus simple qu'une écriture a réellement eu lieu.

**Le nettoyage est garanti, pas espéré.** La suppression se fait dans le mécanisme de libération, de
sorte qu'un test en échec ne laisse pas de base derrière lui. Sans cela, le serveur accumule les bases
orphelines jusqu'à saturation.

## Exemple commenté

Le nom qui rend une base identifiable et jetable :

```csharp
public static bool IsIsolatedDatabase(string? name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        return false;
    }

    const string prefix = "forge-test-";

    // Comparaison ordinale : aucune équivalence culturelle ne doit pouvoir
    // faire passer un nom pour un autre sur un contrôle de sûreté.
    if (!name.StartsWith(prefix, StringComparison.Ordinal))
    {
        return false;
    }

    // Un suffixe suffisamment long évite qu'une exécution parallèle
    // tombe sur le même nom qu'une autre.
    return name.Length - prefix.Length >= 8;
}
```

Une base par exécution, créée et détruite quoi qu'il arrive :

```csharp
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly string _databaseName = $"forge-test-{Guid.NewGuid():N}";

    public AppDbContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Context = new AppDbContext(BuildConnectionString(_databaseName));

        // Le schéma est produit depuis le modèle : le test valide la correspondance
        // réelle, pas un schéma écrit à la main qui aurait pu diverger.
        await Context.Database.EnsureCreatedAsync();
    }

    // La suppression est dans la libération : un test en échec ne laisse
    // aucune base orpheline derrière lui.
    public async Task DisposeAsync()
    {
        await Context.Database.EnsureDeletedAsync();
        await Context.DisposeAsync();
    }
}
```

Et la vérification faite depuis l'extérieur du contexte qui a écrit :

```csharp
[Fact]
public async Task Commande_Enregistree_RecoitUnIdentifiantDeLaBase()
{
    Order order = new(CustomerId: 1);

    _fixture.Context.Orders.Add(order);
    await _fixture.Context.SaveChangesAsync();

    // Un identifiant strictement positif prouve que la base a bien attribué la clé :
    // c'est le signe le plus simple qu'une écriture a réellement eu lieu.
    Assert.True(HasSavedIdentity(order.Id));

    // Relecture par un contexte neuf : lire depuis celui qui a écrit
    // pourrait retourner l'objet encore suivi en mémoire.
    await using AppDbContext verification = _fixture.CreateContext();
    Assert.NotNull(await verification.Orders.FindAsync(order.Id));
}
```

## Contre-exemple et erreur fréquente

```csharp
public class OrderRepositoryTests
{
    // Base partagée entre tous les tests, et pointant une instance réelle.
    private static readonly AppDbContext Context =
        new("Server=srv-recette;Database=Facturation;...");

    [Fact]
    public async Task Enregistrement_Fonctionne()
    {
        Context.Orders.Add(new Order { CustomerId = 1 });
        await Context.SaveChangesAsync();

        // Lecture depuis le contexte qui a écrit : l'objet peut venir du suivi
        // en mémoire, le test passerait même sans écriture effective.
        Assert.NotNull(Context.Orders.Local.FirstOrDefault());
    }

    [Fact]
    public async Task Comptage_EstCorrect()
    {
        // Dépend de l'état laissé par le test précédent, et des données réelles
        // de la base de recette : le résultat change sans qu'aucun code ne change.
        Assert.Equal(1, await Context.Orders.CountAsync());
    }
}
```

Quatre défauts, dont un inacceptable.

Le contexte pointe une base de recette réelle. Ces tests y écrivent, et rien ne les empêche d'en
supprimer le contenu. C'est la ligne à ne jamais franchir : une base de test est créée pour
l'occasion, et détruite après.

Le contexte statique est partagé par tous les tests, ce qui rend le résultat dépendant de l'ordre
d'exécution — et interdit toute exécution parallèle.

La vérification lit le suivi en mémoire du contexte qui vient d'écrire. Le test passe même si
l'enregistrement n'a jamais atteint la base : il ne vérifie donc pas ce qu'il prétend.

Enfin, `Assert.Equal(1, ...)` suppose une base vide au départ. Ce test échouera dès la seconde
exécution, et son échec n'apprendra rien sur le code.

## Vérification de compréhension

Un test d'intégration passe la première fois puis échoue à chaque exécution suivante. Donnez les deux
causes les plus probables et le geste qui corrige chacune.

:::quiz
id=tests-integration-database-001-check
question=Pourquoi une base en mémoire est-elle un mauvais choix pour tester une contrainte d'unicité ?
option=Parce qu'elle est trop lente pour ce type de vérification
option=Parce qu'elle ne respecte ni les contraintes relationnelles, ni les types, ni les transactions du moteur réel : le test validerait un comportement qui n'existe pas en production
option=Parce qu'elle ne permet pas d'ouvrir plusieurs connexions simultanées
correct=1
success=Correct : c'est le fake qui ment. Le fournisseur utilisé en test d'intégration doit être celui de production, sinon le test porte sur une fiction.
retry=Relisez le passage sur la base en mémoire, et demandez-vous ce qu'une contrainte d'unicité exige pour être vérifiée.
:::

## Exercice guidé

Ouvrez `tests-database-name-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui rend un nom de base sûr : préfixe réservé, unicité du suffixe,
   comparaison employée.
2. Implémentez la vérification avec une comparaison ordinale.
3. Vérifiez la longueur de suffixe exactement à la limite, dans les deux sens.
4. Enchaînez avec `tests-saved-identity-001`, qui traite la preuve qu'une écriture a eu lieu.

## Exercice autonome

Concevez la suite d'intégration d'un dépôt de factures.

Décidez avant d'écrire : ce que vous testez en intégration et ce que vous laissez aux tests unitaires,
la stratégie d'isolation retenue et son coût, le nom de la base, la façon dont vous garantissez la
suppression même en cas d'échec, et la manière dont vous vérifiez qu'une écriture a bien atteint la
base.

## Débogage

Un ticket indique : « Depuis qu'on exécute les tests en parallèle, un test sur trois échoue au
hasard. »

1. **Symptôme** : les échecs apparaissent avec l'exécution parallèle et changent de cible.
2. **Hypothèse** : plusieurs exécutions partagent la même base.
3. **Preuve** : relevez le nom de base utilisé par chaque exécution. Un nom identique confirme.
4. **Prévention** : un nom unique par exécution avec préfixe réservé, et une suppression garantie dans
   le mécanisme de libération.

## Entretien

Question posée à voix haute : *comment testez-vous votre couche d'accès aux données ?*

Une réponse solide distingue ce qui relève de l'intégration de ce qui n'en relève pas, décrit
l'isolation par base jetable, explique pourquoi le fournisseur doit être celui de production, et pose
comme non négociable qu'un test ne vise jamais une base réelle.

## Résumé

- Le test d'intégration prouve ce que vous n'avez pas écrit : schéma, contraintes, requêtes.
- Une base de test est créée pour l'occasion et détruite après, toujours.
- Un préfixe réservé rend le nettoyage sûr ; un suffixe unique rend le parallélisme possible.
- Une base en mémoire ne prouve ni contrainte, ni type, ni transaction.
- Vérifier depuis un contexte neuf, sinon on lit le suivi en mémoire.

## Cartes de révision

Question : quelle stratégie d'isolation est la plus rapide, et quelle est sa limite ? Réponse
attendue : la transaction annulée après chaque test — inutilisable si le code sous test gère ses
propres transactions.

Question : quel signe simple prouve qu'une écriture a atteint la base ? Réponse attendue : un
identifiant strictement positif attribué par la base.

## Test de maîtrise

Sans relire, décrivez la suite d'intégration complète d'un module de commandes : le partage entre
unitaire et intégration, la construction du schéma, le nom de la base et sa garantie de suppression,
la stratégie de remise à l'état initial, la façon de vérifier une écriture, et les deux tests qui
prouvent qu'aucune base réelle ne peut être atteinte.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
