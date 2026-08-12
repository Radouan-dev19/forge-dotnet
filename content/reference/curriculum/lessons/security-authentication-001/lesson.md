# Authentification sans fuite d'identité

## Objectif observable

À la fin de cette leçon, vous saurez distinguer authentification et autorisation, écrire un échec de
connexion qui ne révèle rien, et expliquer pourquoi une comparaison de secret doit prendre le même
temps quel que soit le résultat.

## Prérequis

- Avoir lu `api-openapi-contracts-001` et savoir ce qu'un contrat publie réellement.
- Avoir lu `api-configuration-secrets-errors-001` et savoir d'où vient un secret.

## Intuition

Deux questions distinctes, souvent confondues. *Qui êtes-vous ?* est l'authentification. *Avez-vous le
droit de faire ceci ?* est l'autorisation. Les mélanger produit des systèmes où toute personne
authentifiée peut tout faire — le défaut le plus répandu et le plus coûteux.

La seconde idée est qu'un échec de connexion est une **réponse publique**. Tout ce qu'il contient est
lisible par quelqu'un qui essaie précisément d'apprendre quelque chose.

## Explication

**Le message d'échec est uniforme, toujours.** « Cet utilisateur n'existe pas » et « mot de passe
incorrect » sont deux messages différents, et cette différence est une information exploitable : elle
permet d'énumérer les comptes existants. Un seul message pour les deux causes, et le détail réel va au
journal serveur, jamais dans la réponse.

L'uniformité doit être **complète**. Un message identique mais un temps de réponse différent — parce
qu'on ne hache pas le mot de passe quand l'utilisateur n'existe pas — divulgue la même information par
un autre canal. La correction consiste à effectuer le calcul de vérification dans les deux cas.

**La comparaison de secret prend un temps constant.** Une comparaison de chaînes classique s'arrête au
premier caractère différent. Un attaquant qui mesure ce temps peut reconstruire un secret caractère
par caractère. La plateforme fournit une comparaison à temps fixe : elle est obligatoire pour tout
jeton, empreinte ou clé.

**Un mot de passe ne se chiffre pas, il se hache.** Le chiffrement est réversible, donc une fuite de la
base restitue les mots de passe. Il faut une fonction de hachage **lente et salée**, conçue pour cet
usage. Le sel — une valeur aléatoire par utilisateur — empêche qu'une même valeur produise la même
empreinte pour deux comptes, et rend inutiles les tables précalculées.

Une fonction de hachage rapide, même salée, est un mauvais choix : sa vitesse est exactement ce que
l'attaquant exploite pour essayer des milliards de combinaisons.

**Le porteur d'une preuve d'identité l'envoie dans un en-tête.** Le schéma le plus courant préfixe la
valeur par le mot `Bearer`, suivi d'un espace et de la preuve. Deux conséquences pratiques : le schéma
se vérifie sans distinction de casse, et la **valeur de la preuve ne doit jamais être retournée ni
journalisée**. Un en-tête d'autorisation dans un journal est une fuite complète.

Rappel de `api-routing-rest-001` : une preuve d'identité ne passe jamais par une URL.

**Une redirection après connexion est une porte ouverte.** Rediriger vers une adresse fournie par
l'appelant permet d'envoyer un utilisateur authentifié vers un site contrôlé par un tiers, qui
imitera le vôtre. Seuls les chemins locaux commençant par une barre unique sont acceptables ; une URL
absolue et la forme à double barre — qui désigne un autre hôte — sont refusées.

**Une session a une fin.** Une preuve d'identité porte une date d'expiration, et le renouvellement est
un acte explicite. Une preuve sans expiration volée reste valable pour toujours ; c'est pourquoi la
durée courte accompagnée d'un mécanisme de renouvellement est préférable à la durée longue.

**Ce qui doit être journalisé.** L'échec de connexion, avec l'identifiant tenté et l'origine, pour
détecter une attaque par essais successifs. Jamais le mot de passe, jamais la preuve, jamais un
en-tête d'autorisation. C'est la règle de `api-configuration-secrets-errors-001`, appliquée au point le
plus sensible du système.

## Exemple commenté

La vérification du schéma, sans jamais exposer la valeur :

```csharp
public static bool HasBearerToken(string? header)
{
    if (string.IsNullOrWhiteSpace(header))
    {
        return false;
    }

    const string scheme = "Bearer ";
    // Comparaison sans distinction de casse sur le seul schéma : la valeur qui suit
    // n'est ni retournée, ni journalisée, ni comparée ici.
    if (!header.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    // La preuve doit exister : « Bearer » seul, ou suivi de blancs, ne prouve rien.
    return header[scheme.Length..].Trim().Length > 0;
}
```

L'échec uniforme, qui traite les deux causes de la même façon :

```csharp
public static string LoginFailure(bool userExists, bool passwordMatches)
{
    // Le même message pour les deux causes : rien ne permet de distinguer
    // un compte inexistant d'un mot de passe faux.
    // Le détail réel part au journal serveur, pas dans cette valeur de retour.
    return userExists && passwordMatches ? string.Empty : "Identifiants invalides.";
}
```

La comparaison à temps constant, et le refus de redirection externe :

```csharp
// Une comparaison classique s'arrête au premier écart : sa durée révèle
// combien de caractères sont corrects. Celle-ci parcourt toujours tout.
public static bool TokensMatch(string candidate, string expected) =>
    CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(candidate),
        Encoding.UTF8.GetBytes(expected));

public static bool IsLocalRedirect(string? target)
{
    if (string.IsNullOrWhiteSpace(target))
    {
        return false;
    }

    // Une seule barre en tête désigne un chemin local. Deux barres désignent
    // un autre hôte, et toute URL absolue sort du site : les deux sont refusées.
    return target.StartsWith('/') && !target.StartsWith("//", StringComparison.Ordinal);
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpPost("/login")]
public IActionResult Login(string user, string password, string returnUrl)
{
    User? found = _users.Find(user);
    if (found is null)
    {
        // Message distinct : permet d'énumérer les comptes existants.
        return BadRequest("Utilisateur inconnu.");
    }

    // Comparaison de hachage caractère par caractère : sa durée fuit l'information.
    // Et le hachage employé est rapide, donc peu coûteux à casser hors ligne.
    if (Hash(password) != found.PasswordHash)
    {
        _logger.LogWarning("Mot de passe {Password} refusé pour {User}", password, user);
        return BadRequest("Mot de passe incorrect.");
    }

    // La destination vient de l'appelant : une adresse externe est acceptée telle quelle.
    return Redirect(returnUrl);
}
```

Quatre défauts, tous exploitables séparément.

Les deux messages distincts transforment le point de connexion en outil d'énumération : un attaquant
apprend quels comptes existent avant même d'essayer un mot de passe. Le temps de réponse le trahirait
de toute façon, puisqu'aucun hachage n'est calculé dans la première branche.

`Hash(password) != found.PasswordHash` compare deux chaînes avec un arrêt anticipé. Sur un secret
comparé en direct, cette durée variable suffit à le reconstruire.

La journalisation du mot de passe en clair est la fuite la plus grave : elle survit à l'incident, se
retrouve dans les sauvegardes et dans tout système d'agrégation de journaux.

`Redirect(returnUrl)` accepte n'importe quelle destination. Un lien
`/login?returnUrl=https://site-imitateur` enverra l'utilisateur fraîchement authentifié sur une page
qui imite la vôtre.

## Vérification de compréhension

Un point de connexion répond en deux millisecondes pour un compte inexistant et en cent quarante
millisecondes pour un mot de passe faux. Dites ce que cette différence révèle et comment la supprimer.

:::quiz
id=security-authentication-001-check
question=Pourquoi retourner exactement le même message qu'il s'agisse d'un compte inexistant ou d'un mot de passe faux ?
option=Parce que la norme HTTP impose un message unique pour le statut retourné
option=Parce que deux messages distincts permettent d'énumérer les comptes existants avant même d'essayer un mot de passe
option=Parce qu'un message court se transmet plus vite qu'un message détaillé
correct=1
success=Correct : et l'uniformité doit aussi porter sur le temps de réponse, sans quoi la même information fuit par un autre canal.
retry=Relisez le passage sur le message d'échec, et demandez-vous ce qu'un attaquant apprend de chaque réponse.
:::

## Exercice guidé

Ouvrez `security-bearer-header-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, ce qui constitue un en-tête valide : schéma attendu, casse, séparateur,
   présence effective d'une preuve.
2. Implémentez la vérification en n'exposant jamais la valeur de la preuve.
3. Vérifiez le cas du schéma seul, sans valeur, et celui d'une valeur réduite à des blancs.
4. Enchaînez avec `security-login-message-001`, qui traite l'uniformité de l'échec.

## Exercice autonome

Concevez le point de connexion d'une application interne.

Décidez avant d'écrire : la fonction de hachage retenue et pourquoi, la gestion du sel, le message
d'échec exact, ce qui garantit un temps de réponse comparable dans les deux cas, la durée de validité
de la preuve, ce que vous journalisez et ce que vous refusez de journaliser, et le traitement de la
destination de redirection.

## Débogage

Un ticket indique : « Un audit signale que notre formulaire de connexion permet de deviner les adresses
inscrites. »

1. **Symptôme** : la réponse diffère selon que le compte existe ou non.
2. **Hypothèse** : soit le message diffère, soit le temps de réponse diffère.
3. **Preuve** : envoyez deux tentatives — compte inexistant, mot de passe faux — et comparez le corps,
   le statut et la durée. Un écart sur l'un des trois confirme.
4. **Prévention** : message unique, calcul de vérification exécuté dans les deux branches, et test qui
   compare les deux réponses.

## Entretien

Question posée à voix haute : *que retournez-vous quand une connexion échoue ?*

Une réponse solide donne le message uniforme, explique l'énumération qu'il empêche, mentionne
spontanément le canal temporel, et sait dire ce qui part au journal serveur par opposition à ce qui
part au client.

## Résumé

- Authentification et autorisation sont deux questions distinctes.
- Un échec de connexion est uniforme en message **et** en durée.
- Un secret se compare à temps constant, un mot de passe se hache lentement avec un sel.
- Une preuve d'identité voyage dans un en-tête et ne se journalise jamais.
- Une redirection n'accepte qu'un chemin local à barre unique.

## Cartes de révision

Question : pourquoi une fonction de hachage rapide est-elle un mauvais choix pour un mot de passe ?
Réponse attendue : sa vitesse est exactement ce qui permet d'essayer des milliards de combinaisons hors
ligne.

Question : que désigne une destination commençant par deux barres ? Réponse attendue : un autre hôte —
c'est une redirection externe déguisée en chemin.

## Test de maîtrise

Sans relire, écrivez le traitement complet d'une tentative de connexion : vérification, message
retourné, garanties d'uniformité en message et en durée, forme du stockage du mot de passe, durée de
la preuve émise, contenu exact du journal, traitement de la destination de redirection, et les deux
tests qui prouvent qu'aucune information n'est divulguée.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
