# Asynchrone de bout en bout et annulation

## Objectif observable

À la fin de cette leçon, vous saurez propager un jeton d'annulation depuis le point d'entrée jusqu'à
la dernière dépendance, distinguer une annulation d'un échec, et reconnaître les trois façons de
transformer du code asynchrone en blocage.

## Prérequis

- Avoir lu `api-configuration-secrets-errors-001` et savoir d'où vient un délai configuré.
- Avoir lu `async-fundamentals-001` et savoir ce qu'attend réellement une opération asynchrone.

## Intuition

Un serveur ne gagne rien à être asynchrone si l'un des maillons de la chaîne bloque. Un seul appel
synchrone au milieu d'une pile asynchrone immobilise un fil d'exécution pendant toute l'opération, et
le gain des dix autres maillons disparaît.

L'annulation obéit à la même logique. Un jeton qui n'est pas transmis à la dépendance qui attend
réellement ne sert à rien : le client est parti, et le serveur continue de travailler pour personne.

## Explication

**Le jeton descend, du point d'entrée jusqu'au bas de la pile.** L'infrastructure fournit un jeton lié
à la connexion du client : il est déclenché quand celui-ci abandonne. Chaque méthode asynchrone du
chemin le reçoit en dernier paramètre et le transmet. La chaîne ne vaut que par son maillon le plus
faible : il suffit d'un appel où le jeton n'est pas passé pour que l'annulation s'arrête là.

**Une annulation n'est pas un échec.** Elle signale que le résultat n'intéresse plus personne. La
traiter comme une erreur produit du bruit dans les journaux et des alertes déclenchées par des
utilisateurs qui ont simplement fermé un onglet. Le statut `499` — abandon par le client — n'est pas
normalisé mais largement utilisé ; ce qui compte est de ne pas produire un `500`, qui ferait croire à
un défaut du serveur.

Le piège inverse existe aussi : attraper toute exception d'annulation sans distinguer sa cause. Une
opération annulée par un **délai** est un incident réel — la dépendance est trop lente — alors qu'une
annulation par le client ne l'est pas. Le jeton d'origine permet de les séparer.

**Trois façons de bloquer, toutes fréquentes.** Attendre le résultat d'une tâche par sa propriété de
résultat. Attendre sa fin par une méthode d'attente bloquante. Et le cas le plus discret : un
`async void`, qui ne peut être attendu et dont l'exception ne remonte nulle part — elle termine le
processus. La seule exception légitime à `async void` est un gestionnaire d'événement.

Sur un hôte serveur moderne, ces blocages ne provoquent plus l'interblocage historique, mais ils
consomment un fil du réservoir pendant toute l'attente. En charge, le réservoir s'épuise, les temps de
réponse s'effondrent, et le symptôme — lenteur générale sans erreur — est très difficile à rattacher à
sa cause.

**Un délai est une décision, pas un défaut.** Chaque appel sortant mérite un budget de temps. Sans
lui, une dépendance lente retient vos ressources indéfiniment. Le budget se compose : un jeton lié au
client, un jeton lié au délai, et une source liée qui déclenche à la première des deux causes.

Le budget se répartit aussi. Si la requête entière dispose de deux secondes et que trois appels se
succèdent, chacun ne peut pas en prendre deux. C'est ce que l'exercice de la semaine fait calculer.

**Le parallélisme n'est pas la concurrence.** Lancer trois appels indépendants et attendre les trois
divise le temps total. Mais attention à ce qui n'est pas sûr en usage concurrent : un contexte de base
de données ne supporte pas deux opérations simultanées, et le tenter produit une exception explicite.
La règle : paralléliser des appels **indépendants** vers des ressources **distinctes**.

**Ce qui vit plus longtemps que la requête ne doit pas dépendre d'elle.** Déclencher un traitement
d'arrière-plan avec le jeton de la requête le fera annuler dès la réponse envoyée. Un tel traitement
appartient à un service dédié, avec sa propre portée — comme vu dans `api-di-lifetimes-001`.

## Exemple commenté

La chaîne complète, du point d'entrée à la dépendance :

```csharp
[HttpGet("/orders/{id:int}")]
public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
{
    // Le jeton fourni par l'infrastructure est déclenché si le client abandonne.
    Order? order = await _orders.FindAsync(id, cancellationToken);
    return order is null ? NotFound() : Ok(Map(order));
}

public async Task<Order?> FindAsync(int id, CancellationToken cancellationToken) =>
    // Transmis jusqu'à l'opération qui attend réellement : sans cela, la requête
    // continue de s'exécuter en base alors que plus personne n'attend le résultat.
    await _context.Orders
        .Include(order => order.Lines)
        .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
```

Le budget composé, qui borne un appel sortant :

```csharp
public async Task<string> CallCatalogAsync(string reference, CancellationToken cancellationToken)
{
    // La source liée déclenche à la première des deux causes : abandon du client
    // ou dépassement du budget. Une seule attente couvre les deux.
    using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    budget.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

    try
    {
        return await _http.GetStringAsync($"/items/{reference}", budget.Token);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        // Le jeton d'origine n'est pas déclenché : c'est donc le délai qui a expiré.
        // Cette distinction sépare un incident réel d'un abandon sans conséquence.
        throw new TimeoutException("Le catalogue n'a pas répondu dans le budget imparti.");
    }
}
```

Et la répartition du budget entre appels successifs, telle que l'exercice la demande :

```csharp
public static int RemainingBudget(int totalMilliseconds, int spentMilliseconds)
{
    // Un budget négatif n'a pas de sens : au-delà du total, il ne reste rien.
    ArgumentOutOfRangeException.ThrowIfNegative(totalMilliseconds);
    return Math.Max(0, totalMilliseconds - spentMilliseconds);
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpGet("/reports/{id:int}")]
public IActionResult Get(int id)                       // Signature synchrone : le jeton n'existe pas.
{
    // Attente bloquante : un fil du réservoir est immobilisé pendant tout l'appel.
    Report report = _reports.LoadAsync(id).Result;

    // Trois appels séquentiels, alors qu'ils sont indépendants.
    var lines = _lines.LoadAsync(id).Result;
    var totals = _totals.ComputeAsync(id).Result;

    // Traitement d'arrière-plan lancé sans être attendu : son exception
    // ne remonte nulle part et termine le processus.
    _ = ArchiveAsync(report);

    return Ok(new { report, lines, totals });
}

private async void ArchiveAsync(Report report) => await _archive.StoreAsync(report);
```

Quatre défauts, du plus visible au plus dangereux.

La signature synchrone empêche toute annulation : si le client abandonne, les trois appels
continueront jusqu'au bout, et la base travaillera pour un résultat que personne ne lira.

Les trois `.Result` immobilisent chacun un fil pendant toute l'attente. En charge, le réservoir
s'épuise et l'application ralentit globalement, sans qu'aucune erreur ne soit journalisée : c'est un
défaut qu'on ne trouve qu'en profilant.

Les trois chargements sont indépendants et pourraient s'exécuter en parallèle — sous réserve qu'ils ne
partagent pas le même contexte de base de données.

`async void` est le plus grave. Une exception dans `StoreAsync` ne peut être attrapée par l'appelant :
elle remonte sur le contexte de synchronisation et arrête le processus. La correction est un
`Task ArchiveAsync(...)` confié à un service d'arrière-plan avec sa propre portée.

## Vérification de compréhension

Un traitement d'export dure quarante secondes et le client ferme son navigateur au bout de cinq. Dites
ce qui doit se passer côté serveur, et ce qui se passe réellement si le jeton n'a pas été transmis à
la requête de base de données.

:::quiz
id=api-async-cancellation-001-check
question=Pourquoi lier un jeton de délai au jeton de la requête plutôt que d'utiliser l'un ou l'autre seul ?
option=Parce qu'un jeton seul ne peut pas être passé à plusieurs méthodes
option=Parce que l'opération doit s'arrêter à la première des deux causes — abandon du client ou dépassement du budget — et que le jeton d'origine permet ensuite de savoir laquelle
option=Parce que le jeton de la requête est déclenché automatiquement toutes les trente secondes
correct=1
success=Correct : la source liée couvre les deux causes en une seule attente, et l'état du jeton d'origine sépare l'abandon sans conséquence de l'incident de lenteur.
retry=Relisez le passage sur le budget composé, et demandez-vous comment distinguer ensuite un abandon d'un délai expiré.
:::

## Exercice guidé

Ouvrez `api-cancellation-budget-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit retourner la fonction quand le temps consommé dépasse le
   budget total.
2. Implémentez le calcul en refusant explicitement un budget total négatif.
3. Vérifiez les deux bornes : consommation nulle, et consommation strictement égale au total.
4. Lisez ensuite `content/labs/api-mini-erp/src/ForgeApiLab/Program.cs` pour voir la propagation réelle
   du jeton.

## Exercice autonome

Concevez le traitement d'une requête qui appelle deux services externes, écrit en base, puis déclenche
un envoi de courriel.

Décidez avant d'écrire : quels appels sont parallélisables, le budget de temps de chacun, ce que vous
répondez si un service dépasse son budget, ce qui doit survivre à l'abandon du client, et comment vous
journalisez une annulation sans produire d'alerte.

## Débogage

Un ticket indique : « Aux heures de pointe, toute l'application ralentit, mais aucune erreur n'est
journalisée. »

1. **Symptôme** : dégradation générale corrélée à la charge, sans exception.
2. **Hypothèse** : un appel bloquant dans un chemin asynchrone épuise le réservoir de fils.
3. **Preuve** : recherchez les occurrences d'attente bloquante et de `async void` dans le chemin de
   requête, et observez le nombre de fils actifs sous charge.
4. **Prévention** : rendre le chemin asynchrone de bout en bout, et ajouter une règle d'analyse
   statique qui refuse l'attente bloquante — le sujet de `quality-static-analysis-001`.

## Entretien

Question posée à voix haute : *que se passe-t-il si un client abandonne sa requête en cours de
traitement ?*

Une réponse solide décrit la propagation du jeton jusqu'à la dépendance qui attend, distingue
l'annulation d'un échec dans les journaux et le statut retourné, et sait dire que ce qui doit survivre
à la requête ne peut pas dépendre de son jeton.

## Résumé

- Le jeton descend jusqu'à la dépendance qui attend réellement, sans maillon manquant.
- Une annulation par le client n'est pas un incident ; un délai expiré l'est.
- Attente bloquante et `async void` sont les deux fautes coûteuses.
- Chaque appel sortant mérite un budget de temps, réparti entre les étapes.
- Ne parallélisez que des appels indépendants vers des ressources distinctes.

## Cartes de révision

Question : pourquoi un traitement d'arrière-plan ne doit-il pas utiliser le jeton de la requête ?
Réponse attendue : il serait annulé dès la réponse envoyée.

Question : quel symptôme trahit un chemin asynchrone bloqué ? Réponse attendue : une lenteur générale
sous charge, sans aucune erreur journalisée.

## Test de maîtrise

Sans relire, décrivez le traitement complet d'une requête de facturation appelant trois dépendances :
propagation du jeton, budget de chacune, appels parallélisés ou non et pourquoi, distinction entre
abandon et délai dans les journaux, statut retourné dans chaque cas, et sort du traitement
d'arrière-plan déclenché en fin de requête.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
