# Code d'autorisation avec PKCE : la preuve, le state et le nonce

## Objectif observable

À la fin de cette leçon, vous saurez dérouler le flux complet — de la redirection à l'échange —,
calculer et vérifier la preuve PKCE avec la cryptographie de la bibliothèque standard, et surtout
attribuer sans confusion chacune des trois protections à sa menace : le `code_verifier` contre
l'interception du code, le `state` contre la requête forgée inter-site, le `nonce` contre le
rejeu du jeton d'identité.

## Prérequis

- Avoir lu `oauth-flows-001` : ce flux est celui de tout client avec utilisateur.
- Savoir calculer un condensat et un encodage Base64Url, vus avec les jetons.

## Intuition

Le flux repose sur un objet fragile : le *code d'autorisation*, qui transite par le navigateur —
la partie du trajet que le client ne contrôle pas. Trois menaces distinctes visent ce passage, et
chacune a reçu sa parade dédiée. Tout l'art de la leçon tient en une discipline : ne jamais dire
« sécurise le flux » — dire *quelle* parade bloque *quelle* attaque, car elles ne sont pas
interchangeables.

## Explication

**Le flux, étape par étape.** Le client fabrique un secret éphémère — le `code_verifier` — et en
publie l'empreinte — le `code_challenge`, condensat SHA-256 encodé en Base64Url. Il redirige
l'utilisateur vers le guichet avec cette empreinte, un `state` et un `nonce`. Le guichet
authentifie l'utilisateur, mémorise l'empreinte avec le code qu'il émet, et redirige vers le
client avec ce code et le `state` renvoyé tel quel. Le client échange alors le code par un appel
direct, en joignant le `code_verifier` en clair : le guichet recalcule l'empreinte et compare —
si elle diffère, refus. Les jetons ne sont émis qu'à ce moment, hors du navigateur.

**Ce que la preuve PKCE bloque : l'interception du code.** Le code traverse des zones exposées —
journaux de redirection, historique, applications qui se disputent un même schéma d'adresse sur
mobile. Un voleur de code sans le `code_verifier` ne peut rien en faire : l'empreinte déposée à
l'aller ne correspondra à aucun secret qu'il possède. La force du mécanisme tient à l'asymétrie
temporelle — l'empreinte voyage à l'aller par le navigateur, le secret ne sort qu'à l'échange,
par un canal direct — et au sens unique du condensat : voir l'empreinte ne donne pas le secret.
C'est une preuve de possession jetable, recréée à chaque flux, jamais stockée.

**Ce que le `state` bloque : la fausse réponse.** Le retour du guichet arrive chez le client
comme une simple requête entrante — et n'importe qui peut fabriquer une requête entrante. Sans
protection, un attaquant fait visiter à sa victime une adresse de rappel contenant *son propre
code à lui* : la victime se retrouve connectée au compte de l'attaquant sans le savoir — et tout
ce qu'elle y enregistre lui appartient. Le `state` est un jeton anti-falsification : imprévisible,
lié à la session du navigateur qui a *initié* le flux, renvoyé tel quel par le guichet, vérifié
et consommé au retour. Une réponse sans `state` connu, ou avec un `state` déjà consommé, est une
réponse qui ne répond à aucune question posée — elle se refuse. Sa consommation à usage unique
est essentielle : un `state` réutilisable redevient rejouable.

**Ce que le `nonce` bloque : le rejeu du jeton d'identité.** Le `nonce` part avec la demande
d'autorisation mais arrive ailleurs : le guichet le grave *dans le jeton d'identité signé*. À la
réception, le client vérifie que le jeton porte le `nonce` de sa propre demande. Un jeton
d'identité volé dans un autre flux — parfaitement signé, non expiré — porte le `nonce` d'une
autre demande : refusé. La confusion classique consiste à croire que `state` et `nonce` se
doublonnent ; ils opèrent sur deux objets différents — le `state` authentifie *la réponse de
redirection*, le `nonce` authentifie *le jeton d'identité* — et l'un ne peut pas remplacer
l'autre : le `state` ne voyage pas dans le jeton, le `nonce` ne protège pas la redirection.

**Les gardes du guichet.** Trois disciplines complètent le flux côté serveur : le code est à
usage strictement unique — sa seconde présentation doit être refusée et peut révoquer ce qui a
été émis, car elle signale un vol ; l'adresse de rappel se compare exactement à celle
enregistrée — pas de préfixe, pas de sous-domaine générique ; et l'empreinte se compare en temps
constant, comme toute comparaison de secret.

## Exemple commenté

La preuve PKCE tient en quelques lignes de bibliothèque standard — c'est le cœur de l'exercice
guidé :

```csharp
using System.Security.Cryptography;
using System.Text;

// Côté client, à l'aller : l'empreinte publiée est le condensat du secret gardé.
public static string ComputeChallenge(string codeVerifier)
{
    byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
    return Convert.ToBase64String(hash)
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');
}

// Côté guichet, à l'échange : recalculer et comparer en temps constant.
public static bool VerifyChallenge(string codeVerifier, string storedChallenge)
{
    string recomputed = ComputeChallenge(codeVerifier);
    return CryptographicOperations.FixedTimeEquals(
        Encoding.ASCII.GetBytes(recomputed),
        Encoding.ASCII.GetBytes(storedChallenge));
}
```

Le secret est en ASCII par contrat de la norme — son alphabet est restreint — et l'encodage
Base64Url sans remplissage est celui des jetons, déjà pratiqué en semaine quatorze.

## Contre-exemple et erreur fréquente

Le code fautif vérifie le `state` mais le laisse réutilisable :

```csharp
// FAUTIF : le state est vérifié par simple appartenance, jamais consommé.
if (!knownStates.Contains(returnedState))
{
    return Refuse();
}
// ... le flux continue, et knownStates garde le state pour toujours.
```

Le symptôme est invisible en test — le premier passage réussit, les suivants aussi — et c'est
précisément le défaut : une adresse de rappel complète, capturée dans un journal ou un
historique, se rejoue à l'infini puisque son `state` reste « connu ». La correction tient dans
la consommation atomique :

```csharp
// CORRIGÉ : vérifier ET consommer en un geste ; la seconde présentation échoue.
if (!pendingStates.Remove(returnedState))
{
    return Refuse();
}
```

`Remove` rend faux si l'élément n'y était plus : l'appartenance et l'usage unique se vérifient
en une opération, sans intervalle exploitable entre les deux.

## Vérification de compréhension

Avant le quiz, répondez à voix haute : un jeton d'identité volé hier est présenté à votre client
aujourd'hui, signature valide — laquelle des trois protections le refuse, et grâce à quelle
propriété ?

:::quiz
id=oauth-pkce-001-check
question=Que protège le state, et que protège le nonce ?
option=Les deux protègent contre le vol du code d'autorisation, le nonce en secours du state
option=Le state authentifie la réponse de redirection auprès du client ; le nonce authentifie le jeton d'identité — deux objets, deux rejeux distincts
option=Le state chiffre la redirection ; le nonce sale le condensat PKCE
correct=1
success=Exact : le state lie la réponse à la demande initiée par ce navigateur ; le nonce, gravé dans le jeton signé, lie le jeton à cette demande-ci. Aucun des deux ne fait le travail de l'autre.
retry=Relisez les deux sections centrales : sur quel objet chaque protection agit-elle — la requête de retour, ou le jeton ?
:::

## Exercice guidé

Ouvrez l'exercice `security-pkce-challenge-001` dans `/practice`, puis procédez ainsi.

1. Écrivez d'abord la validation du secret — l'alphabet et les bornes de longueur du contrat.
2. Implémentez le condensat puis l'encodage Base64Url sans remplissage, dans cet ordre.
3. Comparez en temps constant, et justifiez ce choix en une phrase dans votre réflexion.
4. Prédisez le verdict de chaque cas visible, notamment celui dont la longueur est à la borne.

## Exercice autonome

Écrivez le pseudocode du *retour* de redirection côté client : les vérifications dans l'ordre —
`state` consommé, code présent, échange, `nonce` du jeton — et, pour chacune, la ligne de journal
que vous écririez en cas de refus, sans jamais y inscrire une valeur secrète.

## Débogage

Un ticket indique : « Environ un utilisateur sur vingt échoue à la connexion avec
`invalid_grant` à l'échange du code ; recharger la page suffit à réussir. »

1. **Symptôme** : échec intermittent à l'échange, jamais à la redirection.
2. **Hypothèse** : le `code_verifier` est stocké dans un état qui ne survit pas toujours au
   retour — session recréée, stockage vidé — et le secret présenté ne correspond plus à
   l'empreinte déposée.
3. **Preuve** : corréler les échecs avec l'absence de la clé de session du demandeur au moment du
   retour, dans les journaux du client.
4. **Prévention** : stocker le secret dans un état lié au navigateur et survivant à la
   redirection, et tester le flux avec une session neuve entre l'aller et le retour.

## Entretien

Question posée à voix haute : *à quoi servent respectivement code_verifier, state et nonce ?*

Une réponse solide attribue chaque protection à sa menace — interception du code, réponse forgée
inter-site, rejeu du jeton d'identité — en nommant l'objet que chacune authentifie. Elle précise
les propriétés opérationnelles : empreinte à l'aller et secret à l'échange pour la preuve, usage
unique pour le `state`, gravure dans le jeton signé pour le `nonce`. La distinction state-nonce
énoncée sans hésitation est exactement ce qui sépare une réponse apprise d'une réponse comprise.

## Résumé

- L'empreinte du secret part à l'aller ; le secret ne sort qu'à l'échange : le code intercepté
  est inutilisable.
- Le `state`, imprévisible et à usage unique, authentifie la réponse de redirection — c'est la
  parade à la requête forgée inter-site.
- Le `nonce` voyage jusque dans le jeton d'identité signé — c'est la parade au rejeu de ce jeton.
- Code à usage unique, adresse de rappel exacte, comparaisons en temps constant : les gardes du
  guichet.
- Trois protections, trois menaces : elles ne se remplacent pas entre elles.

## Cartes de révision

Question : pourquoi l'interception du code d'autorisation est-elle inutile face à PKCE ?
Réponse attendue : l'échange exige le secret dont seule l'empreinte a voyagé ; le condensat ne
se renverse pas, et le voleur du code n'a jamais vu le secret.

Question : que doit faire le client d'un `state` valide présenté une seconde fois ? Réponse
attendue : refuser — la vérification consomme le `state` en un geste atomique, sinon toute
adresse de rappel capturée serait rejouable.

## Test de maîtrise

Sans relire, écrivez le déroulé complet du flux en huit étapes numérotées, en marquant pour
chaque étape ce qui transite par le navigateur et ce qui transite en direct. Puis remplissez de
mémoire le tableau menace-contre-parade pour l'interception du code, la réponse forgée et le
rejeu du jeton d'identité.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
