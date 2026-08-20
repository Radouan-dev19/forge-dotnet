# Tests HTTP avec une fabrique d'application

## Objectif observable

À la fin de cette leçon, vous saurez démarrer votre application en mémoire pour la tester par de vraies
requêtes HTTP, remplacer ses dépendances externes sans toucher au code de production, et vérifier une
réponse au bon niveau de précision.

## Prérequis

- Avoir lu `tests-integration-database-001` et savoir isoler une base de test.
- Avoir lu `api-validation-problem-details-001` et savoir quelle forme prend une erreur.

## Intuition

Certaines choses n'existent qu'une fois la requête passée par la pile complète : le routage, la
liaison du corps, la validation, l'autorisation, la négociation de format, l'intergiciel d'erreurs.
Appeler la méthode du contrôleur directement ne traverse rien de tout cela — et c'est justement là que
les défauts se logent.

Une fabrique d'application démarre le vrai programme en mémoire, sans port réseau ni serveur. On lui
envoie de vraies requêtes, et on reçoit de vraies réponses.

## Explication

**Ce que ce test couvre, et lui seul.** Le point d'entrée existe-t-il à cette adresse et pour cette
méthode ? Le corps est-il correctement lié ? La validation rejette-t-elle avec le statut attendu et le
format d'erreur normalisé ? L'autorisation refuse-t-elle un appelant sans droit ? L'intergiciel
d'erreurs empêche-t-il bien toute fuite ?

Aucune de ces questions ne se pose au niveau d'un test unitaire, parce qu'aucune ne concerne une règle.

**Ce qu'il ne doit pas couvrir.** Les règles du domaine, testées ailleurs et bien plus vite. Un test
HTTP par cas de calcul rend la suite lente et le diagnostic ambigu. La bonne proportion : beaucoup de
tests de règles, quelques tests d'intégration, un petit nombre de tests HTTP couvrant les chemins et
les erreurs.

**Les dépendances se remplacent au démarrage.** La fabrique permet de reconfigurer les services avant
la construction : remplacer la base réelle par la base jetable de la leçon précédente, l'appel externe
par un fake, l'horloge par une valeur fixée. Ce remplacement se fait dans le code de test, sans aucune
concession dans le code de production — pas de condition « si on est en test » dans le programme, qui
serait un chemin non testé s'exécutant en production.

**Chaque test repart d'un état connu.** L'application démarrée est partagée entre les tests d'une même
classe pour éviter de payer le démarrage à chaque fois. Il faut donc remettre l'état à zéro entre les
tests : vider les tables, réinitialiser les compteurs des fakes. Sans cela, on retrouve exactement le
problème d'état partagé de `tests-xunit-aaa-001`, à une échelle plus grande.

**Vérifier au bon niveau de précision.** Vérifier la famille du statut — un succès, un refus — plutôt
qu'un code exact quand le code exact n'est pas le contrat. Inversement, quand le contrat dit `201` avec
un en-tête de localisation, il faut vérifier les deux : c'est précisément ce que
`api-http-semantics-001` engage.

Sur le corps, vérifier les champs qui font partie du contrat, pas la chaîne complète. Comparer un
document sérialisé caractère par caractère fait échouer le test au premier champ ajouté, alors qu'un
ajout est un changement compatible.

**Les erreurs méritent autant de tests que les succès.** Un corps invalide, une ressource inexistante,
un appelant sans droit, un champ interdit envoyé. Ce sont les cas que personne n'essaie à la main, et
ceux qui divulguent le plus quand ils sont mal traités. Un test qui provoque une exception et vérifie
que la réponse ne contient ni pile, ni chemin, ni nom de table est le garde-fou de
`security-owasp-api-001`.

**L'authentification en test.** Deux approches. Émettre une vraie preuve d'identité de test, ce qui
couvre aussi la chaîne d'authentification. Ou substituer un mécanisme d'authentification de test qui
attribue directement une identité, ce qui est plus simple et suffit quand c'est l'autorisation que
l'on veut éprouver. Dans les deux cas, aucune valeur ressemblant à un secret réel n'apparaît dans le
code de test.

## Exemple commenté

La famille de statut, vérifiée par ses frontières :

```csharp
public static bool IsSuccessStatus(int statusCode)
{
    // La famille des succès va de 200 inclus à 300 exclu. Tester 200 seul
    // laisserait passer une borne fausse : ce sont 199, 200, 299 et 300 qui comptent.
    return statusCode >= 200 && statusCode < 300;
}
```

La fabrique qui remplace les dépendances sans toucher au programme :

```csharp
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"forge-test-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Le vrai enregistrement est retiré puis remplacé : le programme
            // ne contient aucune condition « si on est en test ».
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(BuildConnectionString(_databaseName)));

            // Une horloge fixée rend reproductible tout ce qui dépend du temps.
            services.RemoveAll<IClock>();
            services.AddSingleton<IClock>(new FixedClock(new DateTime(2026, 8, 5)));
        });
    }
}
```

Et le test d'un chemin d'erreur, vérifié au bon niveau :

```csharp
[Fact]
public async Task Creation_AvecCorpsInvalide_RetourneUneErreurNormaliseeSansFuite()
{
    HttpClient client = _factory.CreateClient();

    HttpResponseMessage response = await client.PostAsJsonAsync(
        "/orders", new { customerId = 0, lines = Array.Empty<object>() });

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

    // On vérifie les champs qui font partie du contrat, pas le document entier :
    // un champ ajouté demain est un changement compatible, il ne doit rien casser.
    ValidationProblemDetails? problem =
        await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

    Assert.NotNull(problem);
    Assert.Contains("customerId", problem.Errors.Keys, StringComparer.OrdinalIgnoreCase);

    // Le garde-fou : aucune information interne ne sort, quelle que soit la cause.
    string body = await response.Content.ReadAsStringAsync();
    Assert.DoesNotContain("Exception", body, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("\\", body, StringComparison.Ordinal);
}
```

## Contre-exemple et erreur fréquente

```csharp
[Fact]
public async Task Orders_Fonctionne()
{
    // Une application démarrée par test : le coût de démarrage est payé
    // à chaque fois, et la suite devient trop lente pour être lancée souvent.
    await using ApiFactory factory = new();
    HttpClient client = factory.CreateClient();

    // Aucun nettoyage entre tests : la commande créée par le test précédent
    // est toujours là, et le comptage dépend de l'ordre d'exécution.
    await client.PostAsJsonAsync("/orders", new { customerId = 1 });

    HttpResponseMessage response = await client.GetAsync("/orders");

    // Statut vérifié trop grossièrement : 204 passerait aussi, alors que
    // le contrat annonce un corps.
    Assert.True(response.IsSuccessStatusCode);

    // Corps comparé caractère par caractère : le premier champ ajouté
    // au contrat fera échouer ce test, alors que l'ajout est compatible.
    string body = await response.Content.ReadAsStringAsync();
    Assert.Equal("[{\"orderId\":1,\"customerName\":\"Dupont\",\"total\":120.0}]", body);
}
```

Cinq défauts.

Le nom `Orders_Fonctionne` n'énonce rien. En cas d'échec dans un rapport, il faut ouvrir le code.

La fabrique est reconstruite à chaque test : le démarrage complet de l'application est payé autant de
fois qu'il y a de tests, et la suite finit par ne plus être lancée.

Rien ne remet l'état à zéro. Le test dépend de ce que les précédents ont écrit, donc de leur ordre.

`IsSuccessStatusCode` est trop large ici : le contrat annonce un corps, donc `200`, et un `204`
accidentel passerait sans être vu.

La comparaison du corps entier est trop stricte, exactement à l'inverse : elle transforme un ajout
compatible en échec. La bonne granularité est intermédiaire — désérialiser, puis vérifier les champs
du contrat.

## Vérification de compréhension

Vous ajoutez un champ à une réponse. Dites lesquels de vos tests HTTP doivent échouer, lesquels ne
doivent pas, et ce que cela impose sur la façon de les écrire.

:::quiz
id=tests-api-factory-001-check
question=Pourquoi remplacer les dépendances dans la fabrique de test plutôt que par une condition dans le programme ?
option=Parce qu'une condition dans le programme serait plus lente à l'exécution
option=Parce qu'une condition « si on est en test » crée dans le code de production un chemin qui s'exécute sans jamais être testé
option=Parce que la fabrique ne peut pas lire la configuration de l'application
correct=1
success=Correct : le remplacement vit dans le code de test, et le programme exécuté en test est exactement celui exécuté en production.
retry=Relisez le passage sur le remplacement des dépendances, et demandez-vous ce que devient la branche « production » d'une telle condition.
:::

## Exercice guidé

Ouvrez `tests-success-status-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, les quatre valeurs qui encadrent la famille des succès.
2. Implémentez la vérification en exprimant directement l'intervalle.
3. Vérifiez les deux frontières dans les deux sens, plutôt qu'un seul statut nominal.
4. Enchaînez avec `tests-reset-state-001` pour la remise à l'état initial entre deux tests.

## Exercice autonome

Écrivez la suite HTTP d'une ressource « facture » : lecture, création, refus de validation, accès non
autorisé, ressource inexistante.

Décidez avant d'écrire : ce qui est remplacé dans la fabrique et pourquoi, la stratégie de remise à
zéro entre tests, le niveau de précision de chaque assertion, la façon dont vous fournissez une
identité, et le test qui prouve qu'aucune information interne ne sort en cas d'erreur.

## Débogage

Un ticket indique : « La suite HTTP passe en local et échoue dans la chaîne de construction. »

1. **Symptôme** : le résultat dépend de l'environnement d'exécution.
2. **Hypothèse** : un test dépend de l'état laissé par un autre, et l'ordre diffère ; ou une
   dépendance externe n'est pas remplacée.
3. **Preuve** : exécutez la suite dans un ordre inversé en local, et relevez les dépendances
   effectivement enregistrées au démarrage.
4. **Prévention** : remettre l'état à zéro entre les tests, et remplacer explicitement toute dépendance
   sortante dans la fabrique.

## Entretien

Question posée à voix haute : *comment testez-vous une API de bout en bout sans déployer ?*

Une réponse solide décrit la fabrique en mémoire, explique ce que ce niveau couvre et ce qu'il ne doit
pas couvrir, insiste sur le remplacement des dépendances côté test, et traite la question du niveau de
précision des assertions.

## Résumé

- La fabrique démarre le vrai programme en mémoire, sans port ni serveur.
- Ce niveau couvre routage, liaison, validation, autorisation et erreurs.
- Les règles du domaine se testent ailleurs, plus vite et plus clairement.
- Les dépendances se remplacent côté test, jamais par une condition dans le programme.
- Assertion trop large ou trop stricte : les deux extrêmes sont des défauts.

## Cartes de révision

Question : pourquoi ne pas comparer le corps de réponse caractère par caractère ? Réponse attendue :
un champ ajouté est un changement compatible et ne doit pas faire échouer un test.

Question : que doit vérifier le test d'un chemin d'erreur, en plus du statut ? Réponse attendue : que
le corps ne contient ni pile, ni chemin, ni nom de table.

## Test de maîtrise

Sans relire, écrivez la suite HTTP complète d'une opération « annuler une commande » : les dépendances
remplacées, la remise à l'état initial, les cas couverts — succès, validation, autorisation, absence,
erreur interne — le niveau de précision de chaque assertion, et la justification de ce qui reste
volontairement hors de ce niveau de test.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
