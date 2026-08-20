# Async simple, ordre et annulation

## Objectif observable

À la fin de cette leçon, vous saurez dire ce qu'une méthode asynchrone occupe réellement pendant son
attente, reconnaître les deux constructions qui provoquent un blocage ou perdent une exception, et
propager une annulation de bout en bout.

## Prérequis

- Avoir lu `debug-data-inspection-001` et savoir tenir un journal de diagnostic.
- Savoir écrire une méthode qui retourne une valeur et gérer une exception.

## Intuition

Une `Task` n'est pas un thread. C'est la **promesse d'un résultat futur**. Quand une méthode
asynchrone attend une réponse réseau ou disque, elle ne consomme aucun processeur : le thread qui
l'exécutait est rendu et peut servir une autre requête.

C'est tout l'intérêt : l'asynchrone n'accélère pas une opération isolée. Il permet à un serveur de
traiter mille requêtes en attente avec quelques threads, au lieu d'immobiliser mille threads à ne rien
faire.

## Explication

**Asynchrone n'est pas parallèle.** Le parallélisme exécute plusieurs calculs **en même temps** sur
plusieurs cœurs — c'est ce que fait `Parallel.For` pour du travail processeur. L'asynchrone libère un
thread **pendant une attente** — c'est ce que fait `await` pour de l'entrée-sortie. Confondre les deux
mène à envelopper du calcul pur dans `Task.Run` en croyant l'accélérer, alors qu'on ajoute seulement
un changement de contexte.

Le critère : votre opération attend-elle quelque chose d'extérieur — réseau, disque, base — ou
calcule-t-elle ? Attente : asynchrone. Calcul : parallélisme, et seulement s'il est mesurablement
coûteux.

**`await` préserve ce que le blocage détruit.** Lorsqu'une tâche échoue, `await` relance l'exception
d'origine avec sa pile. `.Result` et `.Wait()` l'encapsulent dans une `AggregateException`, ce qui
oblige à déballer et brouille la lecture de la trace — le problème vu dans
`debug-stacktraces-breakpoints-001`.

Plus grave : ces deux appels **bloquent le thread courant**. Dans un contexte de synchronisation qui
n'autorise qu'un thread — historiquement une application de bureau, aujourd'hui encore certaines
bibliothèques — le thread bloqué est précisément celui dont la continuation a besoin pour reprendre.
Chacun attend l'autre : l'application se fige, sans exception ni trace. C'est le plus coûteux des
défauts asynchrones, parce qu'il ne produit aucun message.

La règle est absolue : `async` de bout en bout. Une méthode asynchrone s'appelle avec `await`, et son
appelant devient asynchrone à son tour, jusqu'au point d'entrée.

**`async void` perd les exceptions.** Une méthode `async void` ne retourne aucune tâche : personne ne
peut l'attendre ni observer son échec. Une exception qui s'y produit remonte au contexte de
synchronisation et termine généralement le processus. Le seul usage légitime est le gestionnaire
d'événement d'interface. Partout ailleurs, retournez `Task`.

**Lancer sans attendre perd le résultat et l'erreur.** Appeler une méthode asynchrone sans `await`
démarre le travail et continue. Si la méthode appelante se termine avant, le travail peut être
interrompu ; si elle lève, personne ne le saura. Le compilateur avertit — cet avertissement se traite,
il ne se supprime pas.

**`Task.WhenAll` pour le vrai parallélisme d'attente.** Trois appels réseau indépendants attendus l'un
après l'autre coûtent la somme des trois durées. Démarrés puis attendus ensemble par `Task.WhenAll`,
ils coûtent la durée du plus lent. Attention à la gestion d'erreur : si plusieurs tâches échouent,
`await` sur `WhenAll` ne relance que **la première** exception. Les autres sont dans la propriété
`Exception` de la tâche, et il faut la consulter explicitement pour ne pas perdre d'information.

**L'annulation se propage, elle ne s'invente pas.** Un `CancellationToken` traverse toute la chaîne
d'appels et se transmet à chaque méthode asynchrone qui l'accepte. Une méthode qui reçoit un jeton et
ne le passe pas à ses propres appels crée un point où l'annulation cesse de fonctionner — sans erreur
visible.

Deux conventions accompagnent le mécanisme. `OperationCanceledException` signale une annulation
**attendue** : elle ne doit pas être journalisée comme une erreur. Et `token.ThrowIfCancellationRequested()`
se place dans les boucles longues, car une annulation entre deux itérations ne serait pas détectée
autrement.

**`ConfigureAwait(false)` dans une bibliothèque.** Par défaut, la continuation tente de reprendre sur
le contexte d'origine. Dans du code de bibliothèque, ce n'est ni nécessaire ni souhaitable :
`ConfigureAwait(false)` évite le retour au contexte, améliore le débit et supprime une des conditions
du blocage décrit plus haut. Sur un hôte serveur moderne, il n'existe pas de contexte de
synchronisation, donc l'effet est faible — mais l'habitude reste bonne pour du code partagé.

## Exemple commenté

```csharp
// Le jeton traverse toute la chaîne : c'est la condition pour que l'annulation fonctionne.
public async Task<IReadOnlyList<Report>> BuildReportsAsync(
    IReadOnlyList<string> customerIds,
    CancellationToken cancellationToken)
{
    ArgumentNullException.ThrowIfNull(customerIds);

    // Les appels sont démarrés puis attendus ensemble : coût = le plus lent, pas la somme.
    Task<Report>[] pending = customerIds
        .Select(id => BuildOneAsync(id, cancellationToken))
        .ToArray();

    Report[] reports = await Task.WhenAll(pending).ConfigureAwait(false);
    return reports;
}

private async Task<Report> BuildOneAsync(string customerId, CancellationToken cancellationToken)
{
    // Le jeton est transmis à l'appel réel : sans cela, l'annulation s'arrêterait ici.
    Customer customer = await _repository.LoadAsync(customerId, cancellationToken).ConfigureAwait(false);

    var lines = new List<ReportLine>();
    foreach (Order order in customer.Orders)
    {
        // Une boucle longue vérifie explicitement : l'annulation entre deux tours passerait sinon inaperçue.
        cancellationToken.ThrowIfCancellationRequested();
        lines.Add(await ProjectAsync(order, cancellationToken).ConfigureAwait(false));
    }

    return new Report(customer.Id, lines);
}
```

Côté appelant, l'annulation attendue se distingue d'une vraie erreur :

```csharp
try
{
    reports = await BuildReportsAsync(ids, cancellationToken);
}
catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
{
    // Annulation demandée : ce n'est pas une erreur, on ne journalise pas en Error.
    return Results.StatusCode(499);
}
```

## Contre-exemple et erreur fréquente

```csharp
public decimal GetTotal(string orderId)
{
    // Blocage : le thread courant attend une continuation qui a peut-être besoin de lui.
    Order order = _repository.LoadAsync(orderId).Result;

    // Lancé sans attendre : si la méthode retourne avant, l'audit peut ne jamais partir,
    // et une exception éventuelle sera perdue sans trace.
    _audit.RecordAsync(orderId);

    return order.Lines.Sum(line => line.Amount);
}

public async void Refresh()   // async void : personne ne peut attendre ni observer l'échec.
{
    await ReloadAsync();
}
```

Trois défauts, du plus silencieux au plus visible.

`.Result` bloque. Dans un contexte à thread unique, l'application se fige définitivement, sans
exception, sans trace, sans journal — le diagnostic se fait au vidage mémoire. Et lorsque l'appel
échoue, l'exception arrive encapsulée dans une `AggregateException`, ce qui masque le type réel.

`_audit.RecordAsync(orderId)` sans `await` produit une tâche que personne n'observe. Si elle lève,
l'exception est silencieusement absorbée. Si l'hôte s'arrête entre-temps, l'audit n'est jamais écrit —
et rien ne le signale.

`async void` sur `Refresh` empêche tout appelant de savoir si le rechargement a réussi. Une exception
qui s'y produit ne peut être attrapée par personne et termine le processus.

La correction est structurelle : rendre `GetTotal` asynchrone, `await` les deux appels, et faire
retourner `Task` à `Refresh`.

## Vérification de compréhension

Expliquez en deux phrases ce que fait le thread pendant qu'une méthode asynchrone attend une réponse
réseau, puis ce qui change si l'on écrit `.Result`.

:::quiz
id=async-fundamentals-001-check
question=Quelle est la conséquence la plus grave d'un appel à .Result sur une tâche dans un contexte de synchronisation à thread unique ?
option=L'opération devient légèrement plus lente à cause d'un changement de contexte supplémentaire
option=Le thread bloqué est celui dont la continuation a besoin pour reprendre : l'application se fige sans exception ni trace
option=La tâche est exécutée deux fois, une fois par l'appelant et une fois par le planificateur
correct=1
success=Correct : chacun attend l'autre. L'absence totale de message rend ce défaut bien plus coûteux qu'une exception.
retry=Relisez le passage sur ce que le blocage détruit : il y a une conséquence sur les exceptions, et une autre, pire, sur le thread lui-même.
:::

## Exercice guidé

Ouvrez `debug-awaited-total-001` dans `/practice`, puis procédez ainsi.

1. Repérez, avant tout code, chaque appel asynchrone non attendu et chaque blocage.
2. Écrivez la signature asynchrone correcte de bout en bout, jeton d'annulation compris.
3. Implémentez, en transmettant le jeton à **chaque** appel qui l'accepte.
4. Vérifiez qu'aucun avertissement du compilateur sur une tâche non attendue ne subsiste.

Le DebugLab `debug-async-001` et le scénario `debug-cancellation-state-001` reprennent ce protocole
sur des dépôts cassés complets.

## Exercice autonome

Écrivez une méthode qui interroge trois services indépendants et retourne un agrégat, avec un délai
maximal global de deux secondes.

Décidez avant de coder : séquentiel ou `Task.WhenAll`, comment vous combinez le délai et l'annulation
de l'appelant, ce que vous retournez si un seul service échoue, et comment vous évitez de perdre les
exceptions des autres.

## Débogage

Un ticket indique : « L'application se fige au clic sur Actualiser, sans message ni journal. »

1. **Symptôme** : gel total, aucune exception, aucune trace — l'absence de message est l'indice.
2. **Hypothèse** : un `.Result` ou un `.Wait()` sur le chemin, dans un contexte à thread unique.
3. **Preuve** : recherchez `.Result`, `.Wait()` et `.GetAwaiter().GetResult()` dans le chemin exécuté ;
   suspendez le processus figé et lisez la pile du thread principal, qui montrera l'attente.
4. **Prévention** : rendre la chaîne asynchrone de bout en bout, et ajouter une règle d'analyse
   statique qui refuse le blocage sur une tâche.

## Entretien

Question posée à voix haute : *quelle différence faites-vous entre asynchrone et parallèle ?*

Une réponse solide oppose la libération d'un thread pendant une attente d'entrée-sortie et l'exécution
simultanée d'un calcul sur plusieurs cœurs. Elle donne un exemple de chaque, et sait dire pourquoi
envelopper du calcul dans une tâche sur un serveur web n'améliore généralement rien.

## Résumé

- Une tâche est une promesse de résultat, pas un thread.
- Asynchrone pour l'attente d'entrée-sortie, parallélisme pour le calcul.
- `.Result` et `.Wait()` bloquent et encapsulent les exceptions : `async` de bout en bout.
- `async void` et les tâches non attendues perdent les erreurs.
- Le jeton d'annulation se transmet à chaque appel, et se vérifie dans les boucles longues.

## Cartes de révision

Question : pourquoi une annulation cesse-t-elle parfois de fonctionner au milieu d'une chaîne ?
Réponse attendue : une méthode a reçu le jeton sans le transmettre à ses propres appels.

Question : que se passe-t-il si plusieurs tâches d'un `Task.WhenAll` échouent ? Réponse attendue :
`await` ne relance que la première ; les autres sont dans la propriété `Exception` de la tâche.

## Test de maîtrise

Sans relire, écrivez une méthode asynchrone qui télécharge N documents avec une concurrence limitée à
quatre, propage l'annulation, et retourne les succès en signalant les échecs sans perdre aucune
exception. Justifiez chaque choix et indiquez ce que votre méthode fait si l'annulation survient à
mi-parcours.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
