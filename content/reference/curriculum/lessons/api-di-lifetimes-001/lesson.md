# Injection de dépendances et durées de vie

## Objectif observable

À la fin de cette leçon, vous saurez choisir une durée de vie à partir de l'état que porte le service,
et vous saurez reconnaître la dépendance captive — le défaut le plus coûteux du sujet, parce qu'il ne
se manifeste qu'en charge.

## Prérequis

- Avoir lu `api-validation-problem-details-001` et savoir situer une règle métier.
- Avoir lu `oop-interfaces-composition-001` et savoir injecter une dépendance par constructeur.

## Intuition

Une durée de vie n'est pas une préférence de style : c'est une réponse à la question *« combien de
temps cet objet doit-il exister, et qui a le droit de le partager ? »*.

La réponse se déduit de **l'état que le service porte**. Aucun état partagé mutable : une seule
instance suffit. De l'état lié à la requête en cours : une instance par requête. De l'état lié à
l'appel : une instance par usage.

## Explication

**Les trois durées de vie.** *Singleton* : une instance pour toute l'application. *Scoped* : une
instance par requête HTTP — ou par portée explicitement créée. *Transient* : une nouvelle instance à
chaque résolution.

Le critère de choix, dans l'ordre. Le service porte-t-il un état propre à la requête — un contexte de
base de données, l'identité de l'appelant, un tampon d'unité de travail ? Alors *scoped*. Est-il sans
état, coûteux à construire, et sûr pour un usage concurrent — un client HTTP, un cache, une table de
correspondance figée ? Alors *singleton*. Sinon *transient*, qui est le choix par défaut le moins
risqué.

**Un singleton est partagé par tous les threads, simultanément.** C'est la conséquence qu'on oublie.
Un champ mutable dans un singleton — un compteur, une liste, un « dernier utilisateur » — sera lu et
écrit par plusieurs requêtes en même temps. En développement, une requête à la fois, rien ne casse.
En production, les valeurs se mélangent, et le défaut est intermittent donc très difficile à
diagnostiquer.

**La dépendance captive.** C'est le piège central. Un service *singleton* qui reçoit par constructeur
un service *scoped* garde la **première** instance obtenue, pour toujours. Le service à durée courte
survit alors bien au-delà de sa portée : un contexte de base de données reste ouvert indéfiniment, son
suivi de modifications grossit, et deux requêtes différentes partagent le même état.

Le conteneur .NET détecte ce cas au démarrage lorsque la validation de portée est activée — elle l'est
par défaut en développement. **Ne la désactivez pas** : c'est ce qui transforme un défaut de production
intermittent en erreur au démarrage.

Quand un singleton a réellement besoin d'un service à durée courte, il ne le prend pas par
constructeur : il reçoit une fabrique de portées, ouvre une portée le temps de l'opération, et la
referme. C'est explicite, et la durée de vie redevient correcte.

**Enregistrer sur l'abstraction, pas sur l'implémentation.** Le service consommateur dépend de
l'interface ; le conteneur sait quelle classe construire. C'est ce qui permet de substituer une
implémentation en test, comme vu dans `oop-interfaces-composition-001`.

**Ce qui ne doit pas se faire dans un constructeur.** Un appel réseau, une lecture de base, une
opération lente. Le constructeur s'exécute pendant la résolution, donc dans le chemin de la requête,
et une exception y est difficile à diagnostiquer. L'initialisation coûteuse se fait au démarrage de
l'application ou paresseusement, jamais à la construction.

**Libérer ce qui doit l'être.** Le conteneur libère les services jetables qu'il a créés, à la fin de
leur portée. Il ne libère pas une instance que vous lui avez fournie déjà construite. Et un service
*transient* jetable résolu depuis une portée reste retenu jusqu'à la fin de cette portée : en résoudre
beaucoup dans une boucle accumule les instances.

## Exemple commenté

Trois enregistrements, chacun justifié par l'état porté :

```csharp
// Sans état, coûteux à construire, sûr en usage concurrent : une seule instance.
builder.Services.AddSingleton<IPriceTable, StaticPriceTable>();

// Porte l'unité de travail de la requête en cours : une instance par requête.
builder.Services.AddScoped<IOrderRepository, EfOrderRepository>();

// Léger, sans état partagé : le choix par défaut le moins risqué.
builder.Services.AddTransient<IOrderNumberGenerator, SequentialOrderNumberGenerator>();
```

Le cas où un singleton a besoin d'un service à durée courte, résolu proprement :

```csharp
// Un service d'arrière-plan vit aussi longtemps que l'application : il est singleton.
public sealed class OrderReminderWorker(IServiceScopeFactory scopeFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Une portée par cycle : le dépôt vit le temps du traitement, puis est libéré.
            // Le prendre par constructeur en ferait une dépendance captive.
            await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
            IOrderRepository repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            await SendRemindersAsync(repository, stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }
}
```

Et la décision, réduite à sa règle :

```csharp
public static string LifetimeFor(bool holdsRequestState, bool statelessAndShared)
{
    // L'état de requête décide en premier : il interdit le partage entre requêtes.
    if (holdsRequestState)
    {
        return "scoped";
    }

    return statelessAndShared ? "singleton" : "transient";
}
```

## Contre-exemple et erreur fréquente

```csharp
public sealed class OrderCache
{
    private readonly IOrderRepository _repository;      // scoped, capturé par un singleton
    private readonly List<Order> _recent = [];          // état mutable partagé entre threads

    public OrderCache(IOrderRepository repository) => _repository = repository;

    public Order Get(int id)
    {
        Order? found = _recent.FirstOrDefault(order => order.Id == id);
        if (found is not null)
        {
            return found;
        }

        Order loaded = _repository.Load(id);
        _recent.Add(loaded);                            // écriture concurrente non protégée
        return loaded;
    }
}

builder.Services.AddSingleton<OrderCache>();
```

Trois défauts, tous invisibles en développement.

`_repository` est *scoped* et capturé par un *singleton* : c'est la dépendance captive. Le contexte de
base de données de la toute première requête reste ouvert pour la durée de vie de l'application. Son
suivi de modifications accumule des entités, la mémoire croît, et toutes les requêtes suivantes
lisent l'état figé de la première.

`_recent` est une liste modifiée depuis plusieurs threads sans aucune synchronisation. Les écritures
concurrentes peuvent corrompre sa structure interne — la conséquence n'est pas seulement une donnée
fausse, mais une boucle infinie ou une exception dans le parcours.

Et le cache ne borne rien : il grandit jusqu'à saturation.

La correction : injecter une fabrique de portées plutôt que le dépôt, utiliser une structure conçue
pour l'accès concurrent, et borner la taille. Ou, plus simplement, employer le cache mémoire fourni
par la plateforme, qui traite déjà les trois problèmes.

## Vérification de compréhension

Pour un service qui lit l'identité de l'appelant courant et un service qui contient une table de taux
de change figée, dites quelle durée de vie vous choisissez pour chacun et pourquoi.

:::quiz
id=api-di-lifetimes-001-check
question=Un service à instance unique reçoit par constructeur un service à durée de vie de requête. Que se passe-t-il ?
option=Le conteneur crée une nouvelle instance du service court à chaque appel de méthode
option=La première instance obtenue est retenue pour toute la vie de l'application : le service court survit à sa portée et son état est partagé entre requêtes
option=Une exception est levée à chaque requête suivant la première
correct=1
success=Correct : c'est la dépendance captive. La validation de portée du conteneur la détecte au démarrage — d'où l'intérêt de ne jamais la désactiver.
retry=Relisez le passage sur la dépendance captive : la question est de savoir ce que devient l'instance courte une fois capturée.
:::

## Exercice guidé

Ouvrez `api-di-lifetime-choice-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, la question que vous posez en premier pour trancher.
2. Implémentez la décision en respectant l'ordre de priorité entre les critères.
3. Vérifiez le cas où les deux indicateurs sont vrais simultanément.
4. Lisez ensuite `content/labs/api-mini-erp/src/ForgeApiLab/Program.cs` pour voir des enregistrements
   réels.

## Exercice autonome

Vous devez enregistrer cinq services : un client HTTP vers un service externe, un dépôt de commandes,
un générateur d'identifiants, un cache de taux de change rafraîchi toutes les heures, et un service
qui expose l'utilisateur courant.

Décidez pour chacun la durée de vie et justifiez-la en une phrase. Indiquez lequel pose un risque de
dépendance captive et comment vous le résolvez.

## Débogage

Un ticket indique : « La consommation mémoire du service croît régulièrement et ne redescend qu'au
redémarrage. »

1. **Symptôme** : croissance continue, sans corrélation avec le trafic instantané.
2. **Hypothèse** : un service à instance unique retient un service à durée courte, dont l'état
   s'accumule.
3. **Preuve** : vérifiez que la validation de portée est bien active au démarrage, et inspectez les
   constructeurs des services enregistrés en instance unique.
4. **Prévention** : remplacer la dépendance captive par une fabrique de portées, et ne jamais
   désactiver la validation de portée.

## Entretien

Question posée à voix haute : *comment choisissez-vous entre les trois durées de vie ?*

Une réponse solide part de l'état porté par le service plutôt que de réciter les définitions, cite la
dépendance captive comme le piège principal, et sait dire que le conteneur la détecte au démarrage
quand la validation de portée est active.

## Résumé

- La durée de vie se déduit de l'état porté, pas d'une préférence.
- Un service à instance unique est partagé par tous les threads simultanément.
- La dépendance captive fige un service court pour la vie de l'application.
- Un singleton qui a besoin d'un service court ouvre une portée explicite.
- Rien de coûteux ni de faillible dans un constructeur.

## Cartes de révision

Question : pourquoi ne jamais désactiver la validation de portée ? Réponse attendue : elle transforme
un défaut de production intermittent en erreur au démarrage.

Question : quel est le vrai danger d'un champ mutable dans un service à instance unique ? Réponse
attendue : l'accès concurrent non synchronisé, qui peut corrompre la structure et pas seulement la
valeur.

## Test de maîtrise

Sans relire, écrivez les enregistrements d'un service de facturation comportant un dépôt, un client
externe, un cache et un service d'arrière-plan. Justifiez chaque durée de vie, montrez où se situe le
risque de dépendance captive, et écrivez le code qui l'évite.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
