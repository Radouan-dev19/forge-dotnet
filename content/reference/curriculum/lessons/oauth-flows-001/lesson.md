# OAuth 2.0 : les flux, et lequel choisir

## Objectif observable

À la fin de cette leçon, vous saurez nommer les quatre rôles du protocole, choisir le flux adapté
à un client donné en le justifiant par deux propriétés — la présence d'un utilisateur et la
capacité à garder un secret —, et expliquer pourquoi les flux implicite et mot de passe ont été
retirés des recommandations.

## Prérequis

- Avoir lu `security-jwt-validation-001` : les jetons émis par les flux sont ceux qu'on y valide.
- Savoir ce qu'est une redirection HTTP et un paramètre de requête.

## Intuition

OAuth résout un problème de procuration : une application veut agir auprès d'une API au nom de
quelqu'un, sans jamais détenir son mot de passe. Le protocole organise la remise d'une
procuration — le jeton d'accès — par un guichet central, et les *flux* sont les différents
parcours vers ce guichet, chacun adapté à un type de porteur. Choisir un flux, c'est répondre à
deux questions : y a-t-il un humain dans la boucle, et le client sait-il garder un secret ?

## Explication

**Quatre rôles, à nommer sans hésiter.** Le *propriétaire de la ressource* est l'utilisateur dont
les données sont en jeu. Le *client* est l'application qui veut agir pour lui — page web,
application mobile, service. Le *serveur d'autorisation* est le guichet : il authentifie, obtient
le consentement et émet les jetons. Le *serveur de ressource* est l'API qui vérifie le jeton à
chaque appel. La confusion la plus fréquente est entre les deux serveurs : celui qui émet et
celui qui consomme sont deux responsabilités, même quand un produit les héberge ensemble.

**Public ou confidentiel : la propriété qui gouverne tout.** Un client *confidentiel* s'exécute
là où un secret peut vivre — un serveur, un service — et peut donc s'authentifier lui-même auprès
du guichet. Un client *public* s'exécute chez l'utilisateur — navigateur, mobile, poste de
travail — et tout secret qu'on y embarque est extractible : il n'a que son identifiant, jamais de
preuve propre. Cette distinction n'est pas un détail de configuration : elle décide des flux
accessibles et des protections nécessaires.

**Code d'autorisation avec PKCE : le flux des utilisateurs.** Quand un humain est présent, le
client l'envoie au guichet par redirection ; le guichet authentifie, recueille le consentement et
renvoie un *code* à usage unique ; le client échange ce code contre les jetons par un appel
direct, de serveur à serveur. La preuve d'échange PKCE — détaillée dans la leçon suivante — lie
le code au client qui l'a demandé, ce qui rend le flux sûr même pour un client public. La
recommandation moderne est simple : ce flux, avec PKCE, pour *tous* les clients avec utilisateur,
confidentiels compris — la preuve supplémentaire ne coûte rien et ferme l'interception du code.

**Identifiants client : le flux des machines.** Sans utilisateur — tâche planifiée, service qui
appelle un autre service — il n'y a personne à rediriger ni à faire consentir. Le client
confidentiel présente directement son identifiant et son secret au guichet et reçoit un jeton
d'accès à son propre nom. Deux conséquences structurantes : ce flux est réservé aux clients
confidentiels — un client public n'a pas de secret à présenter — et il n'émet pas de jeton
d'identité, puisqu'aucun humain n'est en jeu.

**L'identité gérée est un flux d'identifiants client dont la plateforme garde le secret.** Le
mécanisme d'identité que les plateformes d'hébergement attachent à une ressource — vu avec la
compétence d'identité de cette semaine — n'est pas un autre protocole : c'est exactement ce
flux-là, où la plateforme fabrique, détient et fait tourner la preuve du client à votre place.
Quand une ressource « obtient un jeton sans secret », le secret existe : il vit dans la
plateforme, hors de votre code — ce qui supprime le problème de l'amorçage sans changer la
mécanique.

**Deux flux morts, et les raisons de leur mort.** Le flux *implicite* renvoyait les jetons
directement dans le fragment d'adresse de la redirection : exposés dans l'historique du
navigateur, les journaux, les en-têtes de référence ; sans authentification du client à
l'échange, et sans jeton de rafraîchissement — il n'existait que parce que les navigateurs
d'alors ne savaient pas faire d'appels directs inter-origines. Le flux *mot de passe* faisait
saisir l'identifiant et le mot de passe de l'utilisateur *dans le client* : il détruit l'idée
même de délégation — le client voit le secret qu'OAuth existait pour ne pas lui montrer —,
il casse l'authentification multifacteur et il habitue les utilisateurs à taper leur mot de
passe n'importe où. Les deux survivent dans du code hérité ; les reconnaître pour les migrer
fait partie du métier.

## Exemple commenté

Le choix du flux, encodé comme une décision pure — c'est le noyau de l'exercice guidé :

```csharp
// Deux questions, dans cet ordre : un humain ? un secret gardable ?
public static string ChooseFlow(bool userPresent, bool confidentialClient)
{
    if (userPresent)
    {
        // Avec utilisateur : code d'autorisation + PKCE, client public ou non.
        return "authorization-code-pkce";
    }

    if (confidentialClient)
    {
        // Machine à machine : le client prouve sa propre identité.
        return "client-credentials";
    }

    // Machine sans secret : aucun flux légitime ne couvre ce cas.
    return "refused";
}
```

Le cas refusé mérite le commentaire : une machine incapable de garder un secret n'a pas de flux —
la réponse est de lui donner une identité gérée par la plateforme, pas de contourner.

## Contre-exemple et erreur fréquente

Le code fautif implémente le flux mot de passe « parce que c'est plus simple » :

```csharp
// FAUTIF : le client collecte le mot de passe de l'utilisateur et l'envoie au guichet.
var form = new Dictionary<string, string>
{
    ["grant_type"] = "password",
    ["username"] = usernameFromOurOwnLoginForm,   // Le client voit le secret !
    ["password"] = passwordTypedIntoOurApp,
    ["client_id"] = "orders-web",
};
```

Le symptôme n'est pas technique — tout fonctionne — il est structurel : le client détient
désormais des mots de passe, donc il devient une cible ; l'authentification multifacteur du
guichet ne peut plus s'exécuter ; et chaque application qui copie ce modèle entraîne les
utilisateurs au hameçonnage. La correction est un changement de flux, pas de code : rediriger
vers le guichet et recevoir un code.

```csharp
// CORRIGÉ : l'utilisateur s'authentifie CHEZ le guichet ; le client ne voit qu'un code.
string authorizeUrl = "https://idp.example/authorize?response_type=code"
    + "&client_id=orders-web&redirect_uri=" + Uri.EscapeDataString(callback)
    + "&code_challenge=" + challenge + "&code_challenge_method=S256"
    + "&state=" + state + "&nonce=" + nonce;
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pour un service de facturation qui appelle l'API de stock
chaque nuit, quel flux, et pourquoi pas l'autre ?

:::quiz
id=oauth-flows-001-check
question=Une application mobile — client public — doit agir au nom de l'utilisateur connecté. Quel flux, et pourquoi ?
option=Identifiants client : le mobile présente son secret intégré à l'application
option=Code d'autorisation avec PKCE : un humain est présent, et la preuve d'échange protège un client qui ne peut pas garder de secret
option=Flux implicite : le mobile ne sait pas faire d'appel direct au guichet
correct=1
success=Exact : utilisateur présent donc code d'autorisation, et PKCE remplace le secret que ce client ne peut pas garder — un secret embarqué dans une application s'extrait toujours.
retry=Reprenez les deux questions du choix : qui est présent, et un secret peut-il vivre dans ce client ?
:::

## Exercice guidé

Ouvrez l'exercice `security-oauth-flow-001` dans `/practice`, puis procédez ainsi.

1. Listez les étiquettes de profil que le contrat définit et leur signification.
2. Implémentez d'abord l'analyse du profil — découpage, normalisation — puis la décision, dans
   l'ordre des deux questions de cette leçon.
3. Traitez les profils contradictoires ou incomplets par le verdict prévu au contrat, jamais par
   un choix silencieux.
4. Prédisez le verdict de chaque cas visible avant de soumettre.

## Exercice autonome

Inventoriez trois applications que vous connaissez — une page web avec connexion, un traitement
nocturne, un outil en ligne de commande — et attribuez à chacune : son type de client, le flux
correct, et la phrase qui le justifie. Écrivez ensuite ce que chacune devrait faire de ses jetons
de rafraîchissement.

## Débogage

Un ticket indique : « L'intégration du partenaire échoue avec `unsupported_grant_type` depuis la
mise à jour du guichet. »

1. **Symptôme** : échec à l'échange de jeton, code d'erreur explicite du protocole.
2. **Hypothèse** : le partenaire utilise un flux retiré — mot de passe ou implicite — que le
   guichet vient de désactiver.
3. **Preuve** : le paramètre `grant_type` de la requête en échec, dans les journaux du guichet.
4. **Prévention** : inventorier les flux par client avant toute mise à jour du guichet, et
   traiter chaque `grant_type=password` restant comme une migration à planifier.

## Entretien

Question posée à voix haute : *comment choisissez-vous un flux OAuth, et pourquoi le flux mot de
passe est-il proscrit ?*

Une réponse solide déroule les deux questions — utilisateur présent, secret gardable —, associe
chaque combinaison à son flux ou à un refus, et démonte le flux mot de passe en une phrase : le
client voit le secret que le protocole existait pour lui cacher. Elle mentionne l'identité gérée
comme cas particulier des identifiants client, preuve que les deux mondes se rejoignent.

## Résumé

- Quatre rôles : propriétaire, client, serveur d'autorisation, serveur de ressource.
- Client public ou confidentiel : la capacité à garder un secret décide des flux accessibles.
- Utilisateur présent : code d'autorisation avec PKCE, pour tous les clients.
- Machine à machine : identifiants client — et l'identité gérée en est la forme sans secret
  visible.
- Implicite et mot de passe sont retirés : jetons exposés pour l'un, délégation détruite pour
  l'autre.

## Cartes de révision

Question : quelles deux propriétés d'un client déterminent son flux OAuth ? Réponse attendue :
la présence d'un utilisateur dans la boucle, et la capacité du client à garder un secret —
public contre confidentiel.

Question : pourquoi le flux implicite a-t-il été retiré ? Réponse attendue : il livrait les
jetons dans l'adresse de redirection — historique, journaux, référents — sans authentification
du client à l'échange ni rafraîchissement possible.

## Test de maîtrise

Sans relire, dessinez le parcours complet du code d'autorisation — acteurs, redirections,
échange — puis écrivez pour trois clients de votre choix le flux correct et sa justification en
une phrase chacun. Terminez par les deux raisons de la mort du flux mot de passe.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
