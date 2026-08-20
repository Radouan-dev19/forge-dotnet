# Configuration, secrets et gestion centrale des erreurs

## Objectif observable

À la fin de cette leçon, vous saurez décider si une valeur est un réglage versionnable ou un secret,
faire échouer l'application au démarrage plutôt qu'à la première requête quand la configuration est
invalide, et garantir qu'aucun secret ne peut apparaître dans un journal.

## Prérequis

- Avoir lu `api-di-lifetimes-001` et savoir enregistrer un service avec la bonne durée de vie.
- Avoir lu `files-json-001` et savoir traiter séparément absence, illisibilité et contenu fautif.

## Intuition

Le test qui tranche entre réglage et secret tient en une question : *si cette valeur devenait
publique, faudrait-il la changer ?* Une adresse de service, un délai d'expiration, une taille de page
maximale : non. Un mot de passe, une clé d'interface, une chaîne de connexion contenant des
identifiants : oui.

La seconde idée est qu'une configuration invalide doit **empêcher l'application de démarrer**. Une
application qui démarre puis échoue à la première requête a transformé une erreur de déploiement en
incident de production.

## Explication

**Les sources se superposent dans un ordre défini.** Le fichier de base, puis le fichier propre à
l'environnement, puis les secrets de développement local, puis les variables d'environnement. Chaque
source écrase la précédente pour les clés qu'elle définit. Comprendre cet ordre est nécessaire pour
diagnostiquer un « pourquoi ma valeur n'est pas prise en compte ».

Les variables d'environnement arrivent en dernier parce que ce sont elles que l'exploitation contrôle
au déploiement. La convention de nommage utilise un séparateur à deux points dans la clé, remplacé par
un double tiret bas dans le nom de la variable.

**Un secret ne vit jamais dans le dépôt.** Ni dans un fichier de configuration, ni dans un fichier
d'exemple, ni dans un commentaire, ni dans un test. En développement local, un magasin de secrets hors
du dépôt. En déploiement, une variable d'environnement injectée, ou un fournisseur externe dédié.

Le fichier d'exemple versionné liste les **clés** attendues avec des valeurs manifestement fausses,
jamais avec une valeur réelle. C'est ce que fait `.env.example` dans ce dépôt.

**La configuration typée se valide au démarrage.** Lier une section à un objet, puis valider cet objet
au démarrage, transforme une erreur de déploiement en échec immédiat et lisible. Le message doit
nommer la clé fautive — sans afficher sa valeur, qui peut être sensible.

C'est le même principe que les contraintes de base de données vues dans
`sql-relational-constraints-001` : faire échouer au plus près de la cause.

**Le journal est une surface de fuite.** Journaliser un objet de configuration entier, une chaîne de
connexion, un en-tête d'autorisation ou un corps de requête publie ce que l'on croyait protégé. Les
journaux sont conservés, agrégés, souvent lisibles par plus de monde que la base.

Trois gestes suffisent. Ne jamais journaliser un objet de configuration en bloc. Rédiger les valeurs
sensibles avant écriture. Et journaliser des données structurées avec des champs nommés plutôt qu'une
chaîne interpolée — ce qui permet aussi de filtrer, comme vu dans `observability-correlation-001`.

**La gestion d'erreurs se centralise.** Un intergiciel unique attrape ce qui remonte, journalise la
pile côté serveur avec l'identifiant de corrélation, et retourne une réponse normalisée sans détail
interne. C'est la mécanique décrite dans `api-validation-problem-details-001` ; le lien avec la
configuration est direct : c'est aussi cet intergiciel qui garantit qu'une exception de connexion ne
publiera pas la chaîne fautive.

**Les valeurs qui changent selon l'environnement ne sont pas toutes des secrets.** Une adresse de base
de données sans identifiants, un niveau de journalisation, un indicateur de fonctionnalité : ce sont
des réglages. Les traiter comme des secrets alourdit l'exploitation sans rien protéger.

## Exemple commenté

Une configuration typée, validée au démarrage :

```csharp
public sealed class OrderApiOptions
{
    public const string SectionName = "OrderApi";

    [Required]
    [Url]
    public string CatalogBaseUrl { get; init; } = string.Empty;

    [Range(1, 100)]
    public int MaximumPageSize { get; init; } = 20;

    [Range(1, 60)]
    public int RequestTimeoutSeconds { get; init; } = 10;
}
```

```csharp
// ValidateOnStart déplace l'échec du premier appel vers le démarrage : une erreur de
// déploiement devient visible immédiatement, pas au premier client.
builder.Services
    .AddOptions<OrderApiOptions>()
    .Bind(builder.Configuration.GetSection(OrderApiOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Le fichier versionné ne contient que du non sensible :

```json
{
  "OrderApi": {
    "CatalogBaseUrl": "http://localhost:5099",
    "MaximumPageSize": 20,
    "RequestTimeoutSeconds": 10
  }
}
```

La chaîne de connexion, elle, vient d'une variable d'environnement au déploiement :

```text
ConnectionStrings__Orders=Server=...;Database=...;User Id=...;Password=...
```

Et la rédaction avant journalisation, qui doit être appliquée partout :

```csharp
// La longueur réelle n'est pas révélée : un masque de longueur fixe minimale
// empêche de déduire la taille du secret.
public static string Redact(string? value) =>
    string.IsNullOrEmpty(value) ? string.Empty : new string('*', Math.Max(4, value.Length));

// Journalisation structurée : les champs sont nommés, filtrables, et rien de sensible n'y entre.
_logger.LogInformation(
    "Appel catalogue {BaseUrl} délai {TimeoutSeconds}s clé {ApiKey}",
    options.CatalogBaseUrl,
    options.RequestTimeoutSeconds,
    Redact(apiKey));
```

## Contre-exemple et erreur fréquente

```csharp
public sealed class CatalogClient
{
    private readonly string _apiKey;

    public CatalogClient(IConfiguration configuration, ILogger<CatalogClient> logger)
    {
        // Aucune validation : une clé absente donne une chaîne vide, et l'échec
        // se produira au premier appel, en production, sans message utile.
        _apiKey = configuration["Catalog:ApiKey"] ?? "";

        // L'objet entier part au journal, clé comprise.
        logger.LogInformation("Configuration catalogue : {@Configuration}",
            configuration.GetSection("Catalog").Get<CatalogOptions>());
    }

    public async Task<string> GetAsync(string reference)
    {
        try
        {
            return await _http.GetStringAsync($"{_baseUrl}/items/{reference}?key={_apiKey}");
        }
        catch (Exception exception)
        {
            // L'URL complète, clé comprise, se retrouve dans le message d'exception journalisé.
            _logger.LogError("Échec catalogue : {Message}", exception.Message);
            throw;
        }
    }
}
```

Trois fuites et un défaut de diagnostic.

Le repli sur une chaîne vide masque l'absence de configuration. L'application démarre, et le premier
appel échoue avec une erreur d'authentification incompréhensible. `ValidateOnStart` aurait produit un
message nommant la clé manquante, au démarrage.

`{@Configuration}` sérialise l'objet entier : la clé d'interface part en clair dans le journal, et y
restera aussi longtemps que les journaux sont conservés.

La clé passée en paramètre d'URL est la fuite la plus large : elle apparaît dans les journaux du
serveur distant, dans ceux de tout serveur mandataire, et dans le message de l'exception journalisé
ici même. Une clé s'envoie dans un en-tête.

## Vérification de compréhension

Classez ces quatre valeurs en réglage ou secret, et justifiez : adresse de base de données sans
identifiants, mot de passe de cette base, taille de page maximale, clé d'un service de paiement.

:::quiz
id=api-configuration-secrets-errors-001-check
question=Pourquoi valider la configuration au démarrage plutôt qu'à la première utilisation ?
option=Parce que la validation au démarrage est plus rapide à l'exécution
option=Parce qu'une erreur de déploiement devient un échec immédiat et nommé, au lieu d'un incident de production au premier appel
option=Parce que le système de configuration ne peut plus être relu après le démarrage
correct=1
success=Correct : une application qui démarre avec une configuration invalide a transformé une erreur de déploiement en incident client.
retry=Relisez le passage sur la validation au démarrage, et demandez-vous quand l'erreur devient visible dans chaque cas.
:::

## Exercice guidé

Ouvrez `api-config-key-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui constitue une clé de configuration valide et ce qui doit être
   refusé.
2. Implémentez la composition en refusant explicitement une section ou une clé vide.
3. Vérifiez qu'une valeur réduite à des blancs est traitée comme absente.
4. Ouvrez ensuite `api-secret-redaction-001` pour écrire la rédaction avant journalisation.

## Exercice autonome

Concevez la configuration d'un service qui appelle deux dépendances externes et écrit dans une base.

Décidez avant d'écrire : la liste des clés, lesquelles sont des secrets, la source de chacune en
développement et en déploiement, les règles de validation, le message produit si une clé manque, et ce
que contient le fichier d'exemple versionné.

## Débogage

Un ticket indique : « Le service fonctionne en recette et retourne des erreurs d'authentification en
production. »

1. **Symptôme** : le comportement dépend de l'environnement, pas de la requête.
2. **Hypothèse** : une clé n'est pas définie en production et un repli silencieux masque son absence.
3. **Preuve** : vérifiez au démarrage que la clé est présente — sans en journaliser la valeur — et
   contrôlez l'ordre de superposition des sources.
4. **Prévention** : ajouter la validation au démarrage, supprimer le repli silencieux, et ajouter un
   test qui vérifie que l'application refuse de démarrer sans la clé.

## Entretien

Question posée à voix haute : *comment gérez-vous les secrets dans vos applications ?*

Une réponse solide distingue réglage et secret par un critère, cite une source différente en
développement et en déploiement, mentionne la validation au démarrage, et reconnaît que le journal est
une surface de fuite au moins aussi importante que le dépôt.

## Résumé

- Un secret est une valeur qu'il faudrait changer si elle devenait publique.
- Les sources se superposent ; les variables d'environnement arrivent en dernier.
- Une configuration invalide doit empêcher le démarrage, pas la première requête.
- Le journal fuit autant que le dépôt : rédiger avant d'écrire.
- Une clé passe par un en-tête, jamais par une URL.

## Cartes de révision

Question : que contient un fichier d'exemple versionné ? Réponse attendue : les clés attendues avec
des valeurs manifestement fausses, jamais une valeur réelle.

Question : pourquoi journaliser en données structurées plutôt qu'en chaîne interpolée ? Réponse
attendue : les champs nommés sont filtrables et se prêtent à la rédaction sélective.

## Test de maîtrise

Sans relire, décrivez la configuration complète d'un service de paiement : clés, classement en réglage
ou secret, source par environnement, validation au démarrage, message d'échec, stratégie de rédaction
dans les journaux, et test qui prouve qu'aucun secret ne peut y apparaître.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
