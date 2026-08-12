# Identité gérée plutôt que secret applicatif

## Objectif observable

À la fin de cette leçon, vous saurez décider où vit une valeur selon sa sensibilité et le contexte
d'exécution, expliquer ce qu'une identité gérée supprime réellement, et reconnaître les usages qui
recréent le problème que le magasin de secrets devait résoudre.

## Prérequis

- Avoir lu `azure-data-services-001` et savoir accorder un droit minimal.
- Avoir lu `api-configuration-secrets-errors-001` et savoir classer une valeur.

## Intuition

Le meilleur secret est celui qui n'existe pas. Un mot de passe stocké quelque part doit être créé,
distribué, renouvelé, retiré — et chacune de ces opérations peut échouer ou fuir.

Une identité gérée supprime le secret plutôt que de le protéger : la plateforme atteste l'identité du
service, et la ressource cible accorde l'accès à cette identité. Il n'y a plus rien à stocker, donc
plus rien à faire fuiter.

## Explication

**Trois destinations, un arbre de décision.** La valeur n'est pas sensible : elle vit dans la
configuration, versionnée. Elle est sensible et une identité gérée est disponible : elle disparaît, au
profit d'un accès attesté par la plateforme et d'un magasin de secrets pour ce qui reste. Elle est
sensible sans identité gérée — typiquement en développement local : elle vit dans un magasin de
secrets local, hors du dépôt.

C'est exactement la règle que l'exercice de cette leçon fait écrire, et c'est la même que celle de
`api-configuration-secrets-errors-001`, prolongée jusqu'à la plateforme.

**Ce qu'une identité gérée supprime.** Le stockage du secret, sa distribution vers chaque
environnement, son renouvellement périodique, et la question « qui a encore une copie ». Ce qui reste
est une décision d'autorisation : quelle identité, sur quelle ressource, avec quel rôle. C'est le
moindre privilège de `security-authorization-roles-policies-001`, appliqué à l'infrastructure.

**Le rôle accordé doit être le plus étroit possible.** Une identité qui ne lit que des secrets ne doit
pas pouvoir en écrire. Une identité qui lit une base ne doit pas pouvoir modifier sa structure.
Accorder un rôle large « pour que ça marche » est l'équivalent, côté infrastructure, du compte
administrateur applicatif du contre-exemple précédent.

**Le magasin de secrets reste nécessaire.** Toutes les dépendances ne savent pas déléguer
l'authentification à la plateforme : un service tiers, une licence, une clé d'interface externe. Ces
valeurs vivent dans le magasin, et l'application les lit **à l'exécution** avec son identité gérée.
La chaîne est alors : identité attestée par la plateforme, puis lecture du secret, puis usage — et
aucun secret ne transite par la chaîne de livraison.

**Le développement local n'a pas d'identité de plateforme.** Un magasin de secrets local, hors du
dépôt, est la réponse. Le fichier d'exemple versionné liste les clés attendues avec des valeurs
manifestement fausses. Ce qu'il ne faut pas, c'est un fichier de configuration réel commis « juste
pour l'équipe ».

**Le renouvellement doit être prévu.** Un secret qui ne peut être renouvelé sans interruption ne sera
jamais renouvelé. Le mécanisme habituel autorise deux valeurs valides simultanément le temps de la
bascule. Le noter dans la procédure évite la situation classique : un certificat qui expire un
dimanche parce que personne n'avait de plan.

**La lecture d'un secret se met en cache, avec mesure.** Interroger le magasin à chaque requête ajoute
de la latence et peut atteindre une limite de débit. Le cache doit expirer, sinon une valeur renouvelée
n'est jamais prise en compte — et le service échouera au moment précis où l'ancienne valeur sera
retirée.

**Ce qui annule tout le dispositif.** Lire le secret au démarrage puis le journaliser. Le passer en
variable d'environnement d'un conteneur inspectable, comme le rappelle
`docker-runtime-security-001`. L'écrire dans un fichier de configuration temporaire qui n'est jamais
supprimé. Le magasin protège le stockage, pas les usages.

## Exemple commenté

La décision, dans son ordre :

```csharp
public static string SensitiveValueSource(bool isSensitive, bool managedIdentityAvailable)
{
    // La sensibilité tranche en premier : une valeur banale n'a rien à faire
    // dans un magasin de secrets, où elle ajoute du coût sans rien protéger.
    if (!isSensitive)
    {
        return "configuration";
    }

    // Ensuite le contexte d'exécution. En local, aucune identité de plateforme
    // n'atteste le service : le magasin local hors dépôt est la seule réponse.
    return managedIdentityAvailable ? "key-vault-managed-identity" : "local-user-secrets";
}
```

L'accès à une base sans aucun mot de passe stocké :

```text
# Chaîne de connexion sans secret : l'authentification est attestée par la plateforme.
ConnectionStrings__Orders=Server=srv-prod;Database=Commandes;Authentication=Active Directory Managed Identity;Encrypt=true;

# Et le droit accordé, aussi étroit que possible, côté base :
#   identité « app-commandes » -> lecture et écriture sur le schéma commandes
#   aucune permission de modification de structure, aucun accès aux autres schémas
```

Et la lecture d'un secret résiduel, avec cache expirant :

```csharp
public sealed class ExternalApiKeyProvider(SecretClient client, TimeMachine clock)
{
    private string? _cached;
    private DateTimeOffset _expiresOn;

    public async Task<string> GetAsync(CancellationToken cancellationToken)
    {
        // Le cache expire : sans expiration, une valeur renouvelée ne serait jamais
        // prise en compte, et le service échouerait au retrait de l'ancienne.
        if (_cached is not null && clock.UtcNow < _expiresOn)
        {
            return _cached;
        }

        KeyVaultSecret secret = await client.GetSecretAsync("cle-api-catalogue", cancellationToken);

        _cached = secret.Value;
        _expiresOn = clock.UtcNow.AddMinutes(30);

        // La valeur n'est ni journalisée, ni retournée dans une réponse,
        // ni écrite dans un fichier temporaire.
        return _cached;
    }
}
```

## Contre-exemple et erreur fréquente

```csharp
public sealed class Startup
{
    public void Configure(IConfiguration configuration, ILogger<Startup> logger)
    {
        // Secret lu au démarrage puis journalisé « pour vérifier que ça marche ».
        string key = _vault.GetSecret("cle-api-catalogue").Value;
        logger.LogInformation("Clé catalogue chargée : {Key}", key);

        // Puis écrit dans un fichier temporaire, jamais supprimé, lisible
        // par tout processus du conteneur.
        File.WriteAllText("/tmp/api-key.txt", key);

        // Et exporté en variable d'environnement : visible à l'inspection du conteneur.
        Environment.SetEnvironmentVariable("CATALOG_KEY", key);
    }
}
```

Accompagné, côté infrastructure, de :

```text
Rôle accordé à l'identité de l'application : administrateur de l'abonnement.
Justification consignée : « sinon ça ne marchait pas ».
```

Quatre défauts qui annulent le dispositif.

Le secret journalisé est exposé aussi longtemps que les journaux sont conservés, et ceux-ci sont
généralement lisibles par plus de monde que le magasin lui-même. Le magasin a protégé le stockage ; la
journalisation a rendu cette protection inutile.

Le fichier temporaire persiste tant que le conteneur vit, et il est lisible par tout processus — y
compris un processus issu d'une exécution de code non prévue.

La variable d'environnement est visible à l'inspection du conteneur, exactement ce que
`docker-runtime-security-001` interdit.

Le rôle d'administrateur de l'abonnement est le plus grave : une compromission du service devient une
compromission de toute l'infrastructure. « Sinon ça ne marchait pas » signale qu'aucun diagnostic n'a
été fait sur le rôle réellement nécessaire.

## Vérification de compréhension

Classez ces quatre valeurs et donnez leur destination : l'adresse d'un service interne, la clé d'un
service de cartographie externe, le mot de passe de la base en production, le même mot de passe pour
un développeur travaillant sur son poste.

:::quiz
id=azure-managed-identity-key-vault-001-check
question=Qu'une identité gérée supprime-t-elle exactement ?
option=Le besoin d'autorisation : toute identité gérée accède à toutes les ressources de l'abonnement
option=Le secret lui-même — donc son stockage, sa distribution, son renouvellement et la question de savoir qui en détient encore une copie
option=Le besoin de chiffrer les communications avec la ressource cible
correct=1
success=Correct : ce qui reste est une décision d'autorisation — quelle identité, sur quelle ressource, avec quel rôle, aussi étroit que possible.
retry=Relisez le passage sur ce qu'une identité gérée supprime, et distinguez ce qui disparaît de ce qui reste à décider.
:::

## Exercice guidé

Ouvrez `azure-secret-source-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce que doit produire chacune des quatre combinaisons possibles.
2. Implémentez la décision en respectant l'ordre : sensibilité d'abord, contexte ensuite.
3. Vérifiez le cas d'une valeur non sensible alors qu'une identité gérée est disponible.
4. Ouvrez ensuite `content/labs/azure-operations/` et relevez où les secrets y sont déclarés.

## Exercice autonome

Concevez la gestion des valeurs d'un service qui appelle une interface externe payante, écrit dans une
base et publie des fichiers.

Listez chaque valeur, classez-la, donnez sa destination en production et en développement local, le
rôle accordé à l'identité du service pour chaque ressource, la stratégie de renouvellement avec son
mécanisme de bascule, la politique de cache, et ce que vous vous interdisez explicitement de faire de
ces valeurs.

## Débogage

Un ticket indique : « Le service échoue en production trois mois après sa mise en ligne, sans qu'aucun
déploiement n'ait eu lieu. »

1. **Symptôme** : une panne apparaît sans changement de code.
2. **Hypothèse** : un secret ou un certificat a expiré, ou une valeur renouvelée n'est pas prise en
   compte à cause d'un cache sans expiration.
3. **Preuve** : comparez la date d'expiration des valeurs utilisées et la durée de vie du cache
   applicatif.
4. **Prévention** : prévoir le renouvellement avec deux valeurs valides simultanément, et donner une
   expiration au cache.

## Entretien

Question posée à voix haute : *comment une application accède-t-elle à une base sans mot de passe ?*

Une réponse solide explique l'attestation d'identité par la plateforme, distingue ce qui disparaît de
ce qui reste à décider, insiste sur l'étroitesse du rôle, et sait dire que le magasin de secrets
protège le stockage mais pas les usages.

## Résumé

- Le meilleur secret est celui qui n'existe pas.
- Sensibilité d'abord, contexte d'exécution ensuite : trois destinations.
- Une identité gérée supprime le secret et laisse une décision d'autorisation.
- Le rôle accordé est le plus étroit qui fonctionne, jamais le plus large qui marche.
- Journal, fichier temporaire et variable d'environnement annulent la protection.

## Cartes de révision

Question : pourquoi un cache de secret doit-il expirer ? Réponse attendue : sinon une valeur
renouvelée n'est jamais prise en compte, et le service échoue au retrait de l'ancienne.

Question : que signale la justification « sinon ça ne marchait pas » sur un rôle ? Réponse attendue :
qu'aucun diagnostic n'a été fait sur le droit réellement nécessaire.

## Test de maîtrise

Sans relire, décrivez la gestion complète des valeurs sensibles d'un service : classement de six
valeurs, destination de chacune en production et en local, rôles accordés à l'identité du service,
stratégie de renouvellement et mécanisme de bascule, politique de cache, contrôles interdits, et le
test qui prouve qu'aucun secret ne peut atteindre un journal.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
