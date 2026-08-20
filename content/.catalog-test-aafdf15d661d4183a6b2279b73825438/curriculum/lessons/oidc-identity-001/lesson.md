# OAuth n'est pas OIDC : autorisation, identité et vie des jetons

## Objectif observable

À la fin de cette leçon, vous saurez dire pourquoi un jeton d'accès ne prouve jamais une
identité — et nommer la faille que crée son usage comme preuve de connexion —, valider un jeton
d'identité par ses revendications propres — `nonce`, `azp`, `at_hash` —, et décrire la vie d'un
jeton de rafraîchissement : rotation à chaque usage, détection de vol, révocation.

## Prérequis

- Avoir lu `oauth-pkce-001` : le flux qui émet les jetons dont il est question ici.
- Savoir valider les revendications d'un jeton signé, acquis en semaine quatorze.

## Intuition

OAuth répond à « ce porteur peut-il appeler cette API ? » ; il ne répond pas à « qui est cette
personne ? ». La couche d'identité — OpenID Connect — ajoute un second jeton, destiné au client
et à lui seul, qui répond à la seconde question. Confondre les deux jetons, c'est accepter un
ticket de métro comme pièce d'identité : le ticket prouve un droit de passage, pas un nom.

## Explication

**Le jeton d'accès est une procuration, pas une pièce d'identité.** Son audience est l'API — le
serveur de ressource — et son contenu dit ce que le porteur peut faire : portées, périmètre,
échéance. Rien n'y authentifie le client qui le présente, et c'est voulu : il est fait pour être
*porté*. La faille classique en découle : une application qui « connecte » ses utilisateurs en
acceptant n'importe quel jeton d'accès valide s'expose à la substitution — une application
malveillante obtient légitimement un jeton d'accès de sa victime pour une API anodine, puis le
présente à votre application comme preuve de connexion. Le jeton est valide, signé, non expiré —
et il n'a jamais rien affirmé sur l'application à qui la personne s'était connectée. Toute
« connexion via » un fournisseur qui se contente d'un jeton d'accès reproduit cette faille.

**Le jeton d'identité est la réponse d'OIDC, et il se valide par trois revendications propres.**
Son audience est le *client* — c'est le premier renversement : l'API n'a pas à le voir, il ne
sert qu'à celui qui a initié la connexion. Le `nonce` le lie à la demande précise du client,
comme vu dans la leçon précédente. L'`azp` — la partie autorisée — nomme le client pour lequel
le jeton a été émis : un jeton d'identité émis pour une autre application, même du même
fournisseur, se refuse là. Et l'`at_hash` scelle le couple : c'est l'empreinte tronquée du jeton
d'accès émis dans la même réponse — la moitié gauche du condensat, encodée en Base64Url — qui
garantit que l'accès et l'identité reçus ensemble n'ont pas été mélangés depuis deux flux
différents. Ces trois contrôles s'ajoutent à la chaîne de la semaine quatorze — signature,
émetteur, audience, échéance — ils ne la remplacent pas.

**Le jeton de rafraîchissement est l'état serveur du dispositif.** L'accès est court et
autoporté ; le rafraîchissement est long et *enregistré chez le guichet* — et cette différence
change tout. Enregistré, il se révoque : la déconnexion, le changement de mot de passe, la
compromission d'un appareil se traduisent par une invalidation immédiate côté guichet, ce que
l'accès autoporté ne permettait pas. La *rotation* ajoute la détection de vol : chaque usage
émet un nouveau jeton de rafraîchissement et invalide l'ancien, si bien qu'un jeton volé et
utilisé par le voleur périme celui du légitime — et la présentation d'un jeton déjà tourné
signale la compromission : le guichet révoque alors toute la lignée. La fenêtre de rotation se
calcule : la prochaine durée d'accès est bornée par l'échéance absolue de la session — c'est
l'objet d'un des exercices de la semaine.

**Qui vérifie quoi, en une ligne chacun.** L'API vérifie le jeton d'accès et ne voit jamais les
deux autres. Le client vérifie le jeton d'identité — et ne présente jamais celui-ci à une API.
Le guichet garde et fait tourner le rafraîchissement. Chaque jeton a un destinataire unique, et
la moitié des failles de ce domaine sont des jetons lus par le mauvais destinataire.

## Exemple commenté

Le contrôle d'`at_hash`, la revendication la moins connue des trois — au programme de l'exercice
guidé :

```csharp
using System.Security.Cryptography;
using System.Text;

// L'at_hash du jeton d'identité doit être l'empreinte tronquée du jeton d'accès
// émis avec lui : moitié gauche du SHA-256, en Base64Url sans remplissage.
public static string ComputeAtHash(string accessToken)
{
    byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(accessToken));

    // Seize octets : la moitié gauche du condensat de trente-deux.
    byte[] leftHalf = hash[..16];

    return Convert.ToBase64String(leftHalf)
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');
}
```

La troncature à la moitié gauche vient de la norme — elle dépend de l'algorithme de signature du
jeton d'identité — et l'oublier produit une empreinte deux fois trop longue qui ne correspondra
jamais.

## Contre-exemple et erreur fréquente

Le code fautif « connecte » l'utilisateur sur la foi du jeton d'accès :

```csharp
// FAUTIF : un jeton d'accès valide est traité comme une preuve d'identité.
var payload = ReadTokenPayload(accessToken);
if (IsSignatureValid(accessToken) && !IsExpired(payload))
{
    SignIn(payload.GetString("sub"));   // Substitution possible : rien ne lie ce
}                                        // jeton à NOTRE application.
```

Le symptôme n'apparaît jamais dans les tests — les jetons y viennent tous du bon flux. L'attaque
consiste à apporter un jeton d'accès obtenu *ailleurs* par une application tierce : `sub` est
bien celui de la victime, la signature est bonne, et votre application ouvre la session. La
correction change de jeton et de contrôles :

```csharp
// CORRIGÉ : la connexion exige le jeton d'IDENTITÉ, validé pour CE client.
string verdict = IdTokenVerdict(idToken, expectedNonce, myClientId, accessToken);
if (verdict == "valid")
{
    SignIn(ReadTokenPayload(idToken).GetString("sub"));
}
```

Le `nonce` lie le jeton à la demande, l'`azp` au client, l'`at_hash` à l'accès reçu ensemble :
la substitution échoue aux trois barrières.

## Vérification de compréhension

Avant le quiz, répondez à voix haute : pourquoi la révocation est-elle possible pour le jeton de
rafraîchissement et pas pour le jeton d'accès autoporté ?

:::quiz
id=oidc-identity-001-check
question=Une application accepte comme connexion tout jeton d'accès valide du fournisseur. Quelle attaque devient possible ?
option=Aucune : la signature du fournisseur garantit l'identité du porteur
option=La substitution : un jeton d'accès de la victime, obtenu légitimement par une application tierce, connecte l'attaquant à sa place
option=Le rejeu du state, que seul le jeton d'identité sait bloquer
correct=1
success=Exact : le jeton d'accès n'affirme rien sur l'application destinataire de la connexion. Seul le jeton d'identité — nonce, azp, at_hash — lie l'identité à ce client et à ce flux.
retry=Relisez la première section : à qui s'adresse chaque jeton, et que prouve-t-il exactement ?
:::

## Exercice guidé

Ouvrez l'exercice `security-oidc-idtoken-001` dans `/practice`, puis procédez ainsi.

1. Écrivez l'ordre des trois contrôles du contrat et le verdict rendu par chacun.
2. Implémentez la lecture des revendications, puis le calcul d'empreinte tronquée en dernier —
   c'est le seul contrôle qui demande de la cryptographie.
3. Vérifiez que votre décodage refuse proprement un jeton sans la revendication attendue.
4. Prédisez le verdict des cas visibles, dont celui où seule l'empreinte diverge.

## Exercice autonome

Écrivez la politique de rafraîchissement d'une application de votre choix : durée de l'accès,
durée absolue de la session, rotation ou non, et ce que déclenche la présentation d'un jeton déjà
tourné. Justifiez chaque nombre en une phrase, puis comparez avec les valeurs par défaut d'un
fournisseur que vous connaissez.

## Débogage

Un ticket indique : « Des utilisateurs sont déconnectés en pleine session avec
`invalid_grant` au rafraîchissement, sans expiration en vue ; cela touche surtout ceux qui
utilisent deux onglets. »

1. **Symptôme** : rafraîchissement refusé bien avant l'échéance, corrélé au multi-onglets.
2. **Hypothèse** : rotation activée et course entre onglets — deux rafraîchissements simultanés
   avec le même jeton ; le second présente un jeton déjà tourné, et le guichet révoque la lignée.
3. **Preuve** : dans les journaux du guichet, deux échanges rapprochés du même jeton de
   rafraîchissement, le second en échec.
4. **Prévention** : sérialiser le rafraîchissement côté client — un seul renouvellement en vol,
   partagé entre onglets — et tolérer côté guichet une fenêtre de grâce brève pour le dernier
   jeton tourné.

## Entretien

Question posée à voix haute : *pourquoi un jeton d'accès ne peut-il pas servir de preuve de
connexion ?*

Une réponse solide part des audiences — l'accès s'adresse à l'API, l'identité au client —,
déroule l'attaque par substitution en trois phrases, et cite les trois revendications qui la
bloquent : `nonce`, `azp`, `at_hash`, chacune avec l'objet qu'elle lie. Elle conclut sur la vie
des jetons : accès court et autoporté, rafraîchissement long, enregistré, tourné à chaque usage
et révocable — la seule pièce du dispositif qu'on peut rappeler.

## Résumé

- Le jeton d'accès prouve un droit d'appel ; il n'affirme rien sur l'identité ni sur le client.
- L'accepter comme connexion ouvre la substitution — la faille classique du « login via ».
- Le jeton d'identité s'adresse au client et se valide par `nonce`, `azp` et `at_hash`, en plus
  de la chaîne signature-émetteur-audience-échéance.
- L'`at_hash` est la moitié gauche du condensat du jeton d'accès, en Base64Url.
- Le rafraîchissement est un état serveur : rotation à chaque usage, réutilisation égale
  compromission, révocation possible — c'est lui qui rend la déconnexion réelle.

## Cartes de révision

Question : quelles revendications distinguent la validation d'un jeton d'identité de celle d'un
jeton d'accès ? Réponse attendue : `nonce` — lien à la demande —, `azp` — lien au client — et
`at_hash` — lien au jeton d'accès émis ensemble.

Question : que signale la présentation d'un jeton de rafraîchissement déjà tourné ? Réponse
attendue : une compromission probable — deux porteurs se partagent la lignée — et la réponse
attendue du guichet est la révocation de toute la lignée.

## Test de maîtrise

Sans relire, écrivez le tableau des trois jetons — destinataire, durée, contenu décisif, ce qui
se passe en cas de vol — puis l'attaque par substitution en quatre phrases et les trois
revendications qui la bloquent, chacune avec l'objet qu'elle lie.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
