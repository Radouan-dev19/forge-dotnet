# CORS : le préflight, ce qu'un en-tête autorise, et le joker interdit

## Objectif observable

À la fin de cette leçon, vous saurez expliquer à quoi sert la requête de préflight et ce qui la
déclenche, dire précisément ce qu'un en-tête d'autorisation d'origine permet — et ne permet pas —,
et justifier pourquoi la spécification refuse le joker d'origine dès que la requête porte des
identifiants.

## Prérequis

- Avoir lu `api-http-semantics-001` : les méthodes, dont OPTIONS, et le rôle des en-têtes.
- Savoir ce qu'est une origine — le triplet schéma, hôte, port.

## Intuition

Le partage de ressources entre origines n'est pas une sécurité du serveur : c'est une sécurité du
*navigateur*, qui protège l'utilisateur contre un site malveillant tentant de lire les réponses
d'un autre site où l'utilisateur est connecté. Le serveur ne fait qu'*autoriser* explicitement les
origines qu'il accepte ; le navigateur, lui, refuse par défaut et n'ouvre que ce que le serveur a
nommé. Comprendre CORS, c'est comprendre que le gardien est le navigateur, et que vos en-têtes
sont les instructions que vous lui donnez.

## Explication

**La politique de même origine est le point de départ.** Par défaut, un script chargé depuis une
origine ne peut pas lire la réponse d'une requête vers une autre origine. Sans cette règle,
n'importe quel site pourrait, depuis votre navigateur, appeler votre banque — où vous êtes
connecté — et lire les réponses. CORS est le mécanisme par lequel un serveur *relâche* cette règle
pour des origines qu'il choisit, de façon contrôlée. Le serveur n'ouvre jamais tout : il déclare
qui, quoi, comment.

**Le préflight demande la permission avant d'agir.** Pour les requêtes qui pourraient avoir un
effet — une méthode autre que les plus simples, un en-tête non standard, un type de contenu
particulier —, le navigateur envoie d'abord une requête **OPTIONS** dite de *préflight* : il
annonce la méthode et les en-têtes qu'il *voudrait* utiliser, et attend que le serveur confirme
qu'il les autorise, pour cette origine, avant d'envoyer la vraie requête. C'est une demande de
permission préalable : si le serveur ne confirme pas, la vraie requête n'est jamais envoyée. Les
requêtes dites simples — une lecture basique — sont dispensées de préflight, mais le navigateur
applique quand même la vérification d'origine sur la réponse.

**Ce qu'un en-tête d'autorisation permet, exactement.** L'en-tête par lequel le serveur nomme
l'origine autorisée dit une seule chose : « un script de *cette* origine a le droit de *lire* ma
réponse ». Il n'authentifie rien, n'ouvre aucun droit métier, ne remplace ni jeton ni contrôle
d'accès. C'est la confusion la plus répandue : croire que déclarer une origine « sécurise »
l'API. Non — l'autorisation et l'authentification restent entièrement à votre charge ; CORS ne
fait qu'indiquer au navigateur quelles origines peuvent lire les réponses. Une API sans jeton
reste ouverte à tous les appels directs, CORS ou pas, car CORS n'existe que dans le navigateur.
De même, l'en-tête qui liste les en-têtes exposés ne fait qu'autoriser le script à les *lire* ; il
ne les crée pas.

**Le joker et les identifiants s'excluent, par spécification.** Un serveur peut répondre « toute
origine est autorisée » par un joker — commode pour une ressource vraiment publique. Mais dès que
la requête porte des *identifiants* — un cookie de session, un en-tête d'autorisation —, la
spécification *interdit* au navigateur d'accepter le joker : le serveur doit alors nommer une
origine précise, et déclarer explicitement qu'il accepte les identifiants. La raison est directe :
autoriser à la fois « n'importe quelle origine » et « avec les cookies de l'utilisateur »
rouvrirait exactement la faille que la politique de même origine ferme — n'importe quel site
lirait vos réponses authentifiées. L'exclusion n'est pas une chicane : c'est le verrou qui empêche
de désactiver la protection par mégarde.

**L'écho d'origine mal fait est une porte dérobée.** Pour « autoriser plusieurs origines », la
tentation est de renvoyer telle quelle l'origine reçue dans la requête — l'écho. Fait sans liste
blanche, cela revient à autoriser *toutes* les origines, identifiants compris, en contournant le
verrou du joker : le serveur affirme à chaque site « oui, toi précisément », ce que le navigateur
accepte. L'écho n'est légitime que confronté à une liste fermée d'origines connues ; sinon, c'est
le joker interdit, déguisé.

## Exemple commenté

Décider l'origine à renvoyer, avec le verrou des identifiants — noyau d'un des exercices :

```csharp
// Origine autorisée seulement si elle figure dans la liste fermée ; jamais de joker
// quand la requête porte des identifiants.
public static string ResolveAllowedOrigin(string requestOrigin, string allowlist, bool withCredentials)
{
    var allowed = new HashSet<string>(
        allowlist.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        StringComparer.Ordinal);

    // Origine inconnue : rien n'est autorisé, le navigateur bloquera.
    if (string.IsNullOrWhiteSpace(requestOrigin) || !allowed.Contains(requestOrigin))
    {
        return "";
    }

    // Connue et listée : on renvoie CETTE origine, jamais le joker — d'autant moins
    // avec identifiants, que la spécification interdit d'associer au joker.
    return requestOrigin;
}
```

Renvoyer l'origine exacte plutôt que le joker est correct *parce qu'*elle a été confrontée à la
liste : l'écho contrôlé est sûr, l'écho aveugle est le joker interdit.

## Contre-exemple et erreur fréquente

Le code fautif combine joker et identifiants :

```csharp
// FAUTIF : toute origine autorisée, ET les cookies acceptés.
Response.Headers["Access-Control-Allow-Origin"] = "*";
Response.Headers["Access-Control-Allow-Credentials"] = "true";
```

Le symptôme dépend du navigateur : la plupart *refusent* cette combinaison et bloquent la réponse,
ce qui casse le client légitime sans que la cause soit évidente — « pourtant j'ai tout autorisé ».
Et si un navigateur l'acceptait, ce serait la faille ouverte : n'importe quel site lirait les
réponses authentifiées de l'utilisateur. La correction nomme l'origine et l'adosse à une liste :

```csharp
// CORRIGÉ : origine précise issue d'une liste blanche, compatible avec les identifiants.
string origin = ResolveAllowedOrigin(request.Origin, Allowlist, withCredentials: true);
if (origin.Length > 0)
{
    Response.Headers["Access-Control-Allow-Origin"] = origin;
    Response.Headers["Access-Control-Allow-Credentials"] = "true";
}
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : CORS protège-t-il votre serveur des appels directs d'un
outil en ligne de commande ? Sinon, qui protège-t-il, et de quoi ?

:::quiz
id=api-cors-001-check
question=Pourquoi la spécification interdit-elle d'associer le joker d'origine à l'acceptation des identifiants ?
option=Parce que le joker est plus lent à traiter que le nom d'une origine
option=Parce qu'autoriser toute origine avec les cookies de l'utilisateur rouvrirait la faille que la politique de même origine ferme : n'importe quel site lirait les réponses authentifiées
option=Parce que les cookies ne peuvent techniquement pas voyager vers une autre origine
correct=1
success=Exact : joker plus identifiants revient à ouvrir les réponses authentifiées à tous les sites. La spécification force à nommer une origine précise quand les identifiants sont en jeu.
retry=Rappelez-vous ce que la politique de même origine protège, et ce que « toute origine + cookies » rouvrirait.
:::

## Exercice guidé

Ouvrez l'exercice `api-cors-origin-001` dans `/practice`, puis procédez ainsi.

1. Analysez la liste blanche en un ensemble d'origines exactes.
2. Refusez toute origine absente ou hors liste par une autorisation vide.
3. Ne renvoyez jamais le joker : l'origine autorisée est l'origine demandée, une fois validée.
4. Prédisez le verdict de chaque cas, dont une origine inconnue et une origine listée.

## Exercice autonome

Pour une API que vous imaginez, décidez la politique CORS : quelles origines, quelles méthodes,
quels en-têtes, avec ou sans identifiants ? Écrivez la réponse de préflight complète pour une
origine autorisée, puis pour une origine refusée, et dites ce que le navigateur fait dans chaque
cas.

## Débogage

Un ticket indique : « Notre application web n'arrive plus à appeler l'API depuis hier ; la console
du navigateur parle d'un blocage CORS, mais l'API répond 200 quand on l'appelle directement. »

1. **Symptôme** : blocage côté navigateur, alors que l'appel direct — hors navigateur — réussit.
2. **Hypothèse** : un changement a mis un joker d'origine alors que l'application envoie des
   identifiants, ou a retiré l'origine de l'application de la liste blanche.
3. **Preuve** : inspecter la réponse de préflight OPTIONS — l'en-tête d'origine autorisée et
   l'acceptation des identifiants — et la comparer à l'origine réelle de l'application.
4. **Prévention** : lister explicitement les origines, ne jamais associer joker et identifiants,
   et tester le préflight comme une réponse à part entière.

## Entretien

Question posée à voix haute : *à quoi sert CORS, et pourquoi ne peut-on pas mettre un joker
d'origine sur une API qui utilise des cookies ?*

Une réponse solide situe CORS dans le navigateur — sécurité du client, pas du serveur —, décrit le
préflight comme une demande de permission préalable, et énonce clairement ce que l'en-tête
d'origine autorise : la lecture de la réponse, rien de plus. Elle explique l'exclusion joker /
identifiants par la faille qu'elle prévient, et mentionne l'écho d'origine aveugle comme sa
version déguisée.

## Résumé

- CORS est une protection du navigateur : le serveur autorise, le navigateur applique.
- Le préflight OPTIONS demande la permission avant les requêtes à effet potentiel.
- L'en-tête d'origine autorise la *lecture* de la réponse ; il ne remplace ni authentification ni
  autorisation.
- Joker et identifiants s'excluent : avec cookies ou jeton, il faut nommer une origine précise.
- L'écho d'origine n'est sûr qu'adossé à une liste blanche fermée.

## Cartes de révision

Question : qui applique la protection CORS, et contre quoi protège-t-elle l'utilisateur ? Réponse
attendue : le navigateur l'applique ; elle empêche un site malveillant de lire, depuis le
navigateur de l'utilisateur, les réponses d'un autre site où il est authentifié.

Question : pourquoi l'écho aveugle de l'origine reçue équivaut-il au joker interdit ? Réponse
attendue : renvoyer toute origine reçue sans la confronter à une liste autorise de fait toutes les
origines — identifiants compris — ce que le verrou joker/identifiants existe pour empêcher.

## Test de maîtrise

Sans relire, décrivez le déroulé complet d'une requête inter-origines avec préflight, en marquant
ce que le navigateur envoie et attend à chaque étape. Puis expliquez pourquoi joker et identifiants
s'excluent, et donnez la bonne façon d'autoriser plusieurs origines connues.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
