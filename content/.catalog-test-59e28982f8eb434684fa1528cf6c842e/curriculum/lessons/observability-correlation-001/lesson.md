# Journal structuré et identifiant de corrélation

## Objectif observable

À la fin de cette leçon, vous saurez émettre des journaux exploitables par une machine, relier toutes
les traces d'une même requête par un identifiant unique, et décider quel signal regarder en premier
devant un incident.

## Prérequis

- Avoir lu `azure-managed-identity-key-vault-001` et savoir ce qui ne doit jamais atteindre un
  journal.
- Avoir lu `api-validation-problem-details-001` et savoir ce qu'une réponse d'erreur peut contenir.

## Intuition

Un journal n'est pas écrit pour être lu ligne par ligne : il est écrit pour être **interrogé**. La
question posée en incident est toujours de la même forme : *montre-moi tout ce qui concerne cette
requête*, ou *combien d'erreurs sur ce point d'entrée depuis une heure*.

Une chaîne de texte libre ne répond à aucune des deux. Des champs nommés y répondent
immédiatement.

## Explication

**Journaliser des données, pas des phrases.** Le message porte un modèle et des paramètres nommés ; le
système de journalisation conserve les deux séparément. On peut alors filtrer sur la valeur d'un champ,
agréger par point d'entrée, compter par client. Une chaîne interpolée perd cette structure au moment
même où elle est construite.

**L'identifiant de corrélation relie tout.** Il est créé à l'entrée de la requête — ou repris de
l'appelant s'il en fournit un —, attaché à toutes les traces émises pendant son traitement, propagé
aux appels sortants, et renvoyé au client dans la réponse d'erreur. C'est le mécanisme annoncé par
`api-validation-problem-details-001` : l'utilisateur communique l'identifiant, et le diagnostic devient
possible sans qu'on lui ait rien divulgué.

Sans lui, retrouver les vingt lignes d'une requête parmi des milliers relève de la reconstitution.

**Une portée de journalisation évite la répétition.** Plutôt que d'ajouter l'identifiant à chaque
appel, une portée ouverte au début de la requête attache automatiquement ses champs à tout ce qui est
émis à l'intérieur. Ajouter l'identifiant du client, le point d'entrée et la version du service à cette
portée rend chaque ligne exploitable sans effort d'écriture.

**Trois niveaux suffisent en pratique.** *Information* pour les événements métier significatifs — une
commande créée, un paiement accepté. *Avertissement* pour ce qui est anormal mais traité — une
dépendance lente, une reprise après échec. *Erreur* pour ce qui a échoué et demande une action. Le
niveau de mise au point existe, mais il n'a pas sa place en production continue : son volume noie le
reste et son coût de stockage est réel.

**Le volume est un choix, pas une conséquence.** Journaliser chaque itération d'une boucle produit des
millions de lignes qui coûtent cher et n'apprennent rien. La règle utile : une trace par décision
significative, pas une par instruction.

**Ce qui ne doit jamais y figurer.** Mot de passe, jeton, en-tête d'autorisation, corps de requête
complet, donnée personnelle non nécessaire, numéro de carte. Les journaux sont conservés, agrégés,
souvent lisibles par plus de monde que la base — c'est la règle de
`api-configuration-secrets-errors-001`, et c'est ici qu'elle se joue.

**Trois familles de signaux, à ne pas confondre.** Les *journaux* racontent ce qui s'est passé, en
détail, pour une requête. Les *métriques* comptent et agrègent : taux d'erreur, latence, débit. Les
*traces distribuées* montrent le trajet d'une requête à travers plusieurs services et où le temps a
été passé.

L'ordre de consultation en incident découle de leur nature : une métrique dit **qu'il y a** un
problème et depuis quand, une trace dit **où**, un journal dit **quoi**.

**Les erreurs passent avant la latence.** Devant un incident, un taux d'erreur non nul est un signal
plus fort qu'une latence dégradée : une erreur est un service non rendu, une lenteur est un service
rendu moins bien. C'est cette priorité que l'exercice de cette leçon fait écrire, avec un budget de
latence explicite pour trancher le second cas.

## Exemple commenté

La priorité des signaux, ramenée à sa règle :

```csharp
public static string IncidentSignal(int errorCount, int p95LatencyMs)
{
    // Des mesures négatives ne décrivent aucun état observé : c'est une faute d'appelant.
    if (errorCount < 0 || p95LatencyMs < 0)
    {
        throw new ArgumentOutOfRangeException(nameof(errorCount));
    }

    // Les erreurs d'abord : un service non rendu prime sur un service lent.
    if (errorCount > 0)
    {
        return "errors";
    }

    // Puis la latence, comparée à un budget explicite plutôt qu'à une impression.
    return p95LatencyMs > 750 ? "latency" : "healthy";
}
```

La portée qui attache le contexte à toute la requête :

```csharp
public async Task InvokeAsync(HttpContext context, RequestDelegate next)
{
    // Repris de l'appelant s'il en fournit un, créé sinon : une chaîne d'appels
    // conserve ainsi le même identifiant d'un service à l'autre.
    string correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? context.TraceIdentifier;

    // Tout ce qui est émis à l'intérieur porte automatiquement ces champs :
    // aucune ligne n'a besoin de les répéter.
    using (_logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId,
        ["Endpoint"] = context.Request.Path.Value ?? "/",
        ["ServiceVersion"] = _version,
    }))
    {
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await next(context);
    }
}
```

Et la différence entre une trace exploitable et une phrase :

```csharp
// Exploitable : les champs sont nommés et conservés séparément. On peut filtrer
// sur OrderId, agréger par CustomerId, compter par statut.
_logger.LogInformation(
    "Commande {OrderId} créée pour le client {CustomerId} avec {LineCount} lignes",
    order.Id, order.CustomerId, order.Lines.Count);

// Non exploitable : tout est fondu dans une chaîne. Compter les commandes
// d'un client demanderait d'analyser du texte libre.
_logger.LogInformation($"Commande {order.Id} créée pour le client {order.CustomerId}");
```

## Contre-exemple et erreur fréquente

```csharp
public async Task<IActionResult> CreateAsync(CreateOrderRequest request)
{
    // Le corps complet part au journal : données personnelles, et parfois
    // des champs sensibles que personne n'a pensé à exclure.
    _logger.LogInformation("Requête reçue : " + JsonSerializer.Serialize(request));

    // L'en-tête d'autorisation journalisé : le jeton est exposé aussi longtemps
    // que les journaux sont conservés.
    _logger.LogDebug("En-tête : " + Request.Headers["Authorization"]);

    foreach (OrderLine line in request.Lines)
    {
        // Une trace par itération : des millions de lignes qui n'apprennent rien
        // et qui coûtent en stockage comme en ingestion.
        _logger.LogInformation($"Traitement de la ligne {line.ProductId}");
    }

    try
    {
        return Ok(await _createOrder.ExecuteAsync(request));
    }
    catch (Exception exception)
    {
        // Message seul, sans exception ni contexte : ni la pile, ni l'identifiant
        // de corrélation, ni le point d'entrée. Le diagnostic est impossible.
        _logger.LogError(exception.Message);
        throw;
    }
}
```

Cinq défauts.

La sérialisation du corps complet publie tout ce que le client a envoyé, y compris ce qui n'aurait
jamais dû sortir de la requête.

Le jeton d'autorisation journalisé est une fuite complète : quiconque lit les journaux peut se faire
passer pour l'appelant jusqu'à l'expiration du jeton.

La trace par ligne de commande produit un volume qui coûte cher et qui noie les événements
significatifs. Une trace par décision aurait suffi.

Les chaînes interpolées suppriment la structure : impossible de filtrer sur un identifiant de produit
sans analyser du texte.

Enfin, journaliser `exception.Message` seul perd la pile, le contexte et l'identifiant de corrélation.
L'exception doit être passée telle quelle au système de journalisation, qui sait la conserver
entièrement.

## Vérification de compréhension

Un utilisateur signale une erreur survenue « ce matin ». Décrivez les trois informations que vous lui
demandez et l'ordre dans lequel vous consultez vos signaux.

:::quiz
id=observability-correlation-001-check
question=Pourquoi journaliser avec des paramètres nommés plutôt qu'avec une chaîne interpolée ?
option=Parce que l'interpolation est plus lente à l'exécution que la substitution de modèle
option=Parce que le système conserve modèle et paramètres séparément : on peut filtrer sur un champ et agréger, ce qu'une chaîne déjà construite ne permet plus
option=Parce que les chaînes interpolées ne peuvent pas dépasser une certaine longueur
correct=1
success=Correct : un journal est écrit pour être interrogé. L'interpolation détruit la structure au moment même où elle est construite.
retry=Relisez le passage sur la journalisation structurée, et demandez-vous comment compter les commandes d'un client donné dans chaque cas.
:::

## Exercice guidé

Ouvrez `azure-correlation-signal-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, l'ordre de priorité entre les deux signaux et sa justification.
2. Implémentez la règle en refusant d'abord les mesures négatives.
3. Vérifiez la frontière exacte du budget de latence, dans les deux sens.
4. Ouvrez ensuite `content/labs/azure-operations/` et relevez les champs attachés à chaque trace.

## Exercice autonome

Concevez la journalisation d'un service de paiement.

Décidez avant d'écrire : les événements tracés et leur niveau, les champs attachés à la portée de
requête, la propagation de l'identifiant vers les appels sortants, ce qui est explicitement exclu,
la durée de conservation, et les trois requêtes que vous devez pouvoir écrire en incident.

## Débogage

Un ticket indique : « Un client signale une erreur, nous ne trouvons aucune trace correspondante. »

1. **Symptôme** : l'incident est invisible dans les journaux.
2. **Hypothèse** : aucun identifiant de corrélation n'est renvoyé au client, ou le niveau émis en
   production exclut la trace utile.
3. **Preuve** : provoquez la même erreur et vérifiez ce que contient la réponse et ce qui est
   effectivement émis.
4. **Prévention** : renvoyer l'identifiant dans toute réponse d'erreur, et vérifier que le niveau émis
   en production couvre les erreurs et les avertissements.

## Entretien

Question posée à voix haute : *comment diagnostiquez-vous un incident en production ?*

Une réponse solide distingue journaux, métriques et traces par ce que chacun répond, donne l'ordre de
consultation, place l'identifiant de corrélation au centre, et sait dire ce qui ne doit jamais entrer
dans un journal.

## Résumé

- Un journal est écrit pour être interrogé, donc structuré en champs nommés.
- L'identifiant de corrélation relie toutes les traces d'une requête, et revient au client.
- Une portée attache le contexte sans le répéter à chaque appel.
- Métrique : y a-t-il un problème. Trace : où. Journal : quoi.
- Une erreur prime sur une latence : service non rendu contre service moins bien rendu.

## Cartes de révision

Question : que perd-on à journaliser seulement le message d'une exception ? Réponse attendue : la pile,
le contexte et le rattachement à la requête — donc l'essentiel du diagnostic.

Question : quelle est la règle de volume utile ? Réponse attendue : une trace par décision
significative, jamais une par instruction.

## Test de maîtrise

Sans relire, décrivez le dispositif d'observation complet d'un service : événements tracés et niveaux,
champs de la portée de requête, création et propagation de l'identifiant de corrélation, exclusions
strictes, métriques suivies, ordre de consultation en incident, et les trois requêtes que vous devez
pouvoir écrire immédiatement.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
