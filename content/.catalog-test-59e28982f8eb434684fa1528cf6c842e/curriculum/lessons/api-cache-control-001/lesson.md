# Cache-Control : qui peut garder quoi, combien de temps, et jamais

## Objectif observable

À la fin de cette leçon, vous saurez composer une directive de cache adaptée à la sensibilité et
à la fraîcheur d'une réponse — `public` ou `private`, `max-age`, `no-store`, revalidation —, et
nommer sans hésiter les réponses qui ne doivent jamais être mises en cache, ainsi que la raison
de chaque interdiction.

## Prérequis

- Avoir lu `api-http-semantics-001` : les réponses que l'on met en cache et celles qu'on ne met
  pas.
- Avoir lu `api-etag-concurrency-001` : la revalidation s'appuie sur l'ETag.

## Intuition

Un cache est un pari : garder une réponse pour ne pas la redemander. `Cache-Control` est le
langage par lequel le serveur dit *qui* a le droit de tenir ce pari, *combien de temps*, et
*quand renoncer*. Mal réglé dans un sens, il sert des données périmées ou privées à la mauvaise
personne ; mal réglé dans l'autre, il jette un trafic que le cache aurait absorbé. Le sujet est
un dosage, et le pire réglage est l'absence de réglage — le comportement par défaut, imprévisible
d'un intermédiaire à l'autre.

## Explication

**`public` contre `private` : qui a le droit de garder.** Une réponse `public` peut être stockée
par n'importe quel cache partagé — un cache d'entreprise, un réseau de diffusion — parce qu'elle
est la même pour tous : un catalogue, une page d'aide. Une réponse `private` ne peut être gardée
que par le cache *du client final* — son navigateur — parce qu'elle lui est propre : son panier,
son profil. Confondre les deux est la faille de cache la plus grave : marquer `public` une réponse
personnelle, c'est autoriser un cache partagé à servir les données d'un utilisateur à un autre.
Dans le doute sur une réponse authentifiée, `private` est le défaut sûr.

**`max-age` : combien de temps, sans redemander.** La directive fixe en secondes la durée pendant
laquelle la réponse est *fraîche* — utilisable sans consulter le serveur. Passé ce délai, elle
devient *périmée* et devra être revalidée. Le choix de la durée est un arbitrage : long, il
soulage le serveur mais risque de servir de l'ancien ; court, il garde les données à jour au prix
du trafic. On l'accorde à la volatilité réelle du contenu — des minutes pour ce qui bouge, des
heures ou des jours pour ce qui est quasi immuable, comme un actif versionné par son nom.

**La revalidation évite de retransférer l'inchangé.** Quand une réponse périmée porte un ETag, le
cache ne rejette pas sa copie : il demande au serveur « est-ce toujours cette empreinte ? » par
une requête conditionnelle. Le serveur répond **304** — inchangé, garde ta copie — ou **200**
avec le nouveau contenu. `no-cache` force cette revalidation à *chaque* usage : la copie peut être
gardée, mais jamais servie sans vérifier. C'est le réglage des données qui doivent être à jour
mais changent rarement — on paie une petite requête conditionnelle, pas un transfert complet. La
continuité avec la leçon ETag est directe : le cache réutilise exactement le mécanisme
conditionnel qui y était décrit.

**`no-store` : ne garder aucune trace.** Radicalement différent de `no-cache`, souvent confondu :
`no-store` interdit d'écrire la réponse où que ce soit — ni disque, ni mémoire, ni cache
intermédiaire. C'est le réglage des données sensibles dont la seule *persistance* est un risque :
un relevé bancaire, un jeton, une réponse contenant un secret. `no-cache` dit « garde mais
revalide » ; `no-store` dit « ne garde pas du tout ». Employer l'un pour l'autre laisse traîner
des données sensibles dans un cache, ou revalide inutilement des données publiques.

**Ce qui ne doit jamais être mis en cache, et pourquoi.** Trois familles. Les réponses
*personnelles servies par un cache partagé* — sauf à les marquer `private`. Les réponses
*sensibles* — `no-store`, car leur persistance est le risque. Et les *réponses aux méthodes qui
changent l'état* — un POST qui crée, un DELETE : les mettre en cache servirait une réponse d'action
à une action qui n'a pas eu lieu. La règle générale : ne mettez en cache que ce dont l'ancienneté
est tolérable et la divulgation inoffensive.

## Exemple commenté

Composer la directive à partir de la nature de la réponse — noyau d'un des exercices :

```csharp
// La sensibilité prime : rien à garder. Sinon, partagé ou privé selon le destinataire.
public static string CacheDirective(string sensitivity, int maxAgeSeconds)
{
    if (string.Equals(sensitivity, "sensitive", StringComparison.Ordinal))
    {
        // La seule persistance est un risque : on interdit tout stockage.
        return "no-store";
    }

    // Contenu personnel : seul le cache du client final peut le garder.
    if (string.Equals(sensitivity, "personal", StringComparison.Ordinal))
    {
        return $"private, max-age={maxAgeSeconds}";
    }

    // Contenu public : les caches partagés peuvent le servir à tous.
    return $"public, max-age={maxAgeSeconds}";
}
```

L'ordre des tests encode une priorité : la sensibilité l'emporte sur tout le reste, car une
donnée sensible marquée `public` par erreur est la pire des issues.

## Contre-exemple et erreur fréquente

Le code fautif met en cache partagé une réponse personnelle :

```csharp
// FAUTIF : le profil de l'utilisateur connecté, marqué cacheable pour tous.
Response.Headers["Cache-Control"] = "public, max-age=3600";
return currentUserProfile;   // Un cache partagé servira CE profil au visiteur suivant.
```

Le symptôme est le plus inquiétant du domaine : un utilisateur voit les données d'un autre, servies
depuis un cache intermédiaire qui a cru la réponse commune. Cela ne se reproduit pas en
développement, où il n'y a ni cache partagé ni second utilisateur. La correction restreint le
stockage au client final :

```csharp
// CORRIGÉ : personnel, donc private ; seul le navigateur du propriétaire le garde.
Response.Headers["Cache-Control"] = "private, max-age=60";
return currentUserProfile;
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : `no-cache` et `no-store` — lequel autorise de garder la
copie, et lequel l'interdit ? Pour quel type de donnée choisit-on chacun ?

:::quiz
id=api-cache-control-001-check
question=Quelle directive convient à une réponse contenant un relevé de compte, dont la seule persistance dans un cache serait un risque ?
option=public, max-age=0 — publiable mais immédiatement périmé
option=no-cache — la copie est gardée mais revalidée à chaque usage
option=no-store — aucune écriture nulle part, ni disque ni mémoire ni cache intermédiaire
correct=2
success=Exact : une donnée sensible ne doit laisser aucune trace stockée ; no-store interdit tout stockage, là où no-cache autoriserait à garder la copie.
retry=Distinguez garder-mais-revalider de ne-pas-garder : lequel des deux protège une donnée dont la persistance elle-même est le danger ?
:::

## Exercice guidé

Ouvrez l'exercice `api-cache-directive-001` dans `/practice`, puis procédez ainsi.

1. Rangez les natures de réponse par ordre de priorité : la sensibilité d'abord.
2. Composez la directive correspondante, en insérant `max-age` seulement là où le cache est permis.
3. Vérifiez qu'aucune réponse sensible ne peut recevoir `public` par un chemin détourné.
4. Prédisez la directive rendue pour chaque cas visible.

## Exercice autonome

Pour cinq réponses d'une API que vous imaginez — un catalogue, un profil, un jeton, un actif
versionné, une confirmation de paiement —, écrivez la directive de cache exacte et la raison en une
phrase. Repérez celles qui ne doivent jamais être mises en cache et dites pourquoi.

## Débogage

Un ticket indique : « Après déconnexion, l'écran d'accueil affiche parfois le nom d'un autre
utilisateur pendant quelques secondes. »

1. **Symptôme** : données d'un tiers brièvement visibles, après déconnexion, de façon
   intermittente.
2. **Hypothèse** : une réponse personnelle a été marquée cacheable partagé — ou sans directive,
   laissée au défaut d'un intermédiaire — et un cache la sert entre deux sessions.
3. **Preuve** : inspecter l'en-tête `Cache-Control` de la réponse personnelle ; l'absence de
   `private`, ou la présence de `public`, confirme.
4. **Prévention** : marquer `private` toute réponse dépendant de l'identité, `no-store` toute
   réponse sensible, et tester que ces en-têtes sont présents.

## Entretien

Question posée à voix haute : *quelle différence faites-vous entre `no-cache` et `no-store`, et
quand employez-vous chacun ?*

Une réponse solide énonce que `no-cache` autorise à garder mais impose de revalider à chaque usage,
tandis que `no-store` interdit tout stockage, puis rattache chacun à son cas — données à jour mais
peu changeantes pour l'un, données sensibles pour l'autre. Elle distingue `public` de `private` par
le destinataire et cite la faille du profil personnel marqué public. La revalidation la relie à
l'ETag.

## Résumé

- `public` pour ce qui est commun à tous, `private` pour ce qui est propre au client final.
- `max-age` fixe la durée de fraîcheur, accordée à la volatilité réelle du contenu.
- `no-cache` garde mais revalide à chaque usage — via l'ETag ; `no-store` interdit tout stockage.
- Ne jamais mettre en cache partagé une réponse personnelle, ni stocker une réponse sensible.
- L'absence de directive est un réglage : celui, imprévisible, du défaut de chaque intermédiaire.

## Cartes de révision

Question : pourquoi marquer `public` une réponse personnelle est-il dangereux ? Réponse
attendue : un cache partagé peut alors stocker cette réponse et la servir à un autre utilisateur,
divulguant les données du premier au second.

Question : quand choisit-on `no-store` plutôt que `no-cache` ? Réponse attendue : quand la
persistance même de la donnée est le risque — donnée sensible — car `no-store` interdit tout
stockage, alors que `no-cache` autorise à garder la copie sous condition de revalidation.

## Test de maîtrise

Sans relire, écrivez la directive de cache de six réponses d'une API réaliste, de la plus publique
à la plus sensible, en justifiant chaque choix. Puis expliquez en trois phrases la différence
opérationnelle entre `no-cache` et `no-store` à un collègue qui les emploie comme synonymes.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
