# Valider un jeton JWT : un ordre qui ne se négocie pas

## Objectif observable

À la fin de cette leçon, vous saurez valider un jeton dans l'ordre correct — forme, algorithme,
signature, puis seulement les revendications —, justifier chaque étape par l'attaque qu'elle bloque,
appliquer une tolérance d'horloge aux bornes temporelles, et expliquer pourquoi la durée de vie
courte remplace la révocation qu'un jeton autoporté ne permet pas.

## Prérequis

- Avoir lu `security-jwt-anatomy-001` et savoir décoder les segments d'un jeton.
- Savoir utiliser `System.Security.Cryptography` pour calculer un HMAC.

## Intuition

On vérifie le tampon avant de lire la lettre. Tant que la signature n'est pas prouvée, chaque octet
du jeton — l'algorithme annoncé, la date d'expiration, l'émetteur — est une affirmation que
l'attaquant a pu écrire lui-même. Lire une revendication avant d'avoir vérifié la signature, c'est
demander au faussaire si son passeport est authentique.

## Explication

**L'ordre découle d'un principe unique : ne rien croire d'un texte non authentifié.** Une chaîne de
validation correcte procède ainsi. D'abord la *forme* : trois segments, chacun décodable, du JSON
là où il en faut — un jeton malformé s'arrête ici, sans autre analyse. Ensuite l'*algorithme* :
celui que le serveur impose, comparé à ce que l'en-tête annonce. Puis la *signature*, recalculée
avec la clé du serveur et comparée à celle du jeton. Et seulement alors les *revendications* :
expiration, prise d'effet, émetteur, audience. Toute lecture métier avant la signature travaille
sur des données potentiellement forgées.

**L'algorithme est imposé par le serveur, jamais lu dans l'en-tête.** C'est l'attaque historique la
plus célèbre du format. L'en-tête annonce `alg`, et une implémentation naïve « fait confiance » :
elle lit `alg`, puis applique l'algorithme demandé. Un attaquant écrit alors `"alg": "none"` et
supprime la signature — certaines bibliothèques acceptaient ce jeton sans aucune vérification. La
variante par confusion d'algorithmes est plus subtile : un serveur qui vérifie du RSA se voit
présenter un jeton HMAC signé avec... la clé publique RSA, que l'attaquant connaît par définition.
Dans les deux cas, la faille est la même : laisser l'entrée choisir le mécanisme qui la juge. La
parade tient en une ligne de configuration : le vérificateur n'accepte qu'une liste fermée
d'algorithmes qu'il a lui-même décidée, et rejette tout jeton dont l'en-tête annonce autre chose.

**La comparaison de signatures se fait en temps constant.** Comparer deux condensats octet par
octet en s'arrêtant à la première différence fuit de l'information : le temps de réponse dit
jusqu'où la falsification était correcte. `CryptographicOperations.FixedTimeEquals` parcourt tout,
quel que soit le point de divergence. Le gain est modeste sur un HMAC complet, mais le réflexe doit
être systématique : aucune comparaison de secret par `==` ou `SequenceEqual`.

**Les bornes temporelles se vérifient avec une tolérance d'horloge.** `exp` dit quand le jeton
cesse d'être valable, `nbf` quand il commence à l'être. Deux machines n'ont jamais exactement la
même heure : sans tolérance, un jeton émis par un serveur légèrement en avance est rejeté par un
serveur légèrement en retard, et l'incident est intermittent, donc infernal à diagnostiquer. La
pratique consiste à accepter un écart borné — quelques dizaines de secondes — dans les deux sens :
l'expiration est repoussée de la tolérance, la prise d'effet avancée d'autant. La tolérance
compense la dérive des horloges ; elle n'est pas une extension de durée de vie, et une valeur de
plusieurs minutes doit alerter en revue.

**Audience et émetteur sont obligatoires, pas décoratifs.** Un jeton valide pour la signature et
les dates peut avoir été émis pour un autre service. Sans contrôle d'`aud`, un jeton volé à une API
de consultation ouvre l'API d'administration du même émetteur : c'est le rejeu croisé. Sans
contrôle d'`iss`, un environnement de test qui partage une clé par négligence devient un émetteur
accepté en production. Le vérificateur nomme donc explicitement l'émetteur attendu et sa propre
audience, et rejette tout jeton qui ne les porte pas exactement.

**Un jeton ne se révoque pas ; on organise sa fin de vie autrement.** La force du jeton autoporté —
aucun état serveur — est aussi sa faiblesse : émis, il reste vérifiable jusqu'à `exp`, même si le
compte est désactivé une minute plus tard. Maintenir une liste de jetons bannis réintroduit
exactement l'état que le format promettait d'éviter. La réponse standard est le couple
accès/rafraîchissement : un jeton d'accès à durée courte — minutes, pas heures — qui voyage à
chaque requête, et un jeton de rafraîchissement à durée longue, conservé de façon plus sûre,
présenté à un unique point de contrôle qui, lui, consulte l'état du compte avant de réémettre. La
fenêtre d'exposition d'un jeton d'accès volé se réduit alors à sa durée de vie résiduelle, et la
désactivation d'un compte prend effet au prochain rafraîchissement.

## Exemple commenté

Vérifier une signature HMAC-SHA256 à la main — l'algorithme est fixé par le code, pas par le
jeton :

```csharp
using System.Security.Cryptography;
using System.Text;

public static bool HasValidSignature(string token, string secret)
{
    string[] segments = token.Split('.');
    if (segments.Length != 3)
    {
        return false;
    }

    // La signature attendue se calcule sur « en-tête.charge-utile », point compris,
    // avec la clé que LE SERVEUR détient — rien n'est lu dans l'en-tête du jeton.
    byte[] signedBytes = Encoding.UTF8.GetBytes($"{segments[0]}.{segments[1]}");
    byte[] expected = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), signedBytes);

    byte[] presented;
    try
    {
        presented = DecodeBase64Url(segments[2]);
    }
    catch (FormatException)
    {
        return false;
    }

    // Comparaison en temps constant : jamais == ni SequenceEqual sur un secret.
    return CryptographicOperations.FixedTimeEquals(expected, presented);
}
```

Puis l'expiration avec tolérance, une fois — et seulement une fois — la signature prouvée :

```csharp
public static bool IsStillValid(long expirationUnixSeconds, long nowUnixSeconds, int toleranceSeconds)
{
    // L'expiration est stricte : à l'instant exact d'exp, le jeton est déjà périmé.
    // La tolérance repousse la borne pour absorber la dérive d'horloge entre machines.
    return nowUnixSeconds < expirationUnixSeconds + toleranceSeconds;
}
```

## Contre-exemple et erreur fréquente

Le code fautif lit l'algorithme dans l'en-tête et le laisse piloter la vérification :

```csharp
// FAUTIF : l'entrée choisit le mécanisme qui la juge.
string algorithm = ReadHeaderValue(token, "alg");
if (algorithm == "none")
{
    return true;                       // « Pas de signature à vérifier » : jeton accepté !
}
if (algorithm == "HS256")
{
    return VerifyHmac(token, secret);
}
```

Le symptôme est invisible en test : tous les jetons légitimes portent `HS256` et passent par la
bonne branche. L'attaque, elle, fabrique un jeton dont l'en-tête annonce `none`, sans signature, et
obtient un accès complet — les revendications qu'il contient sont alors entièrement choisies par
l'attaquant, y compris `sub` et les rôles.

La correction inverse la dépendance : le serveur décide, l'en-tête doit s'y conformer.

```csharp
// CORRIGÉ : algorithme fixé côté serveur ; tout écart de l'en-tête est un rejet.
const string RequiredAlgorithm = "HS256";
string announced = ReadHeaderValue(token, "alg");
if (!string.Equals(announced, RequiredAlgorithm, StringComparison.Ordinal))
{
    return false;
}
return VerifyHmac(token, secret);
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : un jeton expiré porte une signature invalide — que doit
retourner votre validateur, « expiré » ou « signature » , et pourquoi l'ordre importe-t-il ?

:::quiz
id=security-jwt-validation-001-check
question=Un validateur lit la revendication exp, constate que le jeton n'est pas expiré, puis vérifie la signature. Quel est le défaut ?
option=Aucun : les deux contrôles sont faits, l'ordre est indifférent
option=La lecture d'exp avant la preuve de signature travaille sur une donnée potentiellement forgée, et toute décision prise à ce stade repose sur du texte non authentifié
option=Le défaut est ailleurs : exp doit se vérifier avec l'heure locale du client, pas celle du serveur
correct=1
success=Exact : avant la signature, la charge utile n'a aucune valeur probante. On peut décoder pour router, jamais pour décider.
retry=Relisez le principe unique de l'ordre : que vaut une revendication tant que la signature n'est pas prouvée ?
:::

## Exercice guidé

Ouvrez l'exercice `security-jwt-order-001` dans `/practice`, puis procédez ainsi.

1. Listez les six verdicts possibles et l'ordre exact dans lequel les contrôles s'enchaînent.
2. Implémentez la forme, puis l'algorithme, puis la signature, en vous interdisant de décoder la
   charge utile avant que la signature ne soit prouvée.
3. Ajoutez les contrôles de revendications dans l'ordre restant, chacun retournant son verdict.
4. Pour chaque cas visible, prédisez le verdict avant de lancer, en notant quel contrôle s'arrête
   en premier.

## Exercice autonome

Écrivez la fonction qui décide si un jeton d'accès doit être rafraîchi : elle reçoit l'expiration,
l'heure courante et la durée totale de vie, et retourne vrai quand il reste moins du tiers de la
durée. Décidez avant de coder : les bornes exactes, le comportement quand l'expiration est déjà
passée, et ce que change une durée totale nulle. Éprouvez ensuite chaque décision par un cas.

## Débogage

Un ticket indique : « Des utilisateurs sont rejetés avec 401 pendant une à deux minutes après la
connexion, puis tout rentre dans l'ordre. Cela ne touche qu'un serveur du parc. »

1. **Symptôme** : rejets brefs, systématiquement juste après l'émission, localisés à une machine.
2. **Hypothèse** : l'horloge du serveur fautif est en retard ; la revendication `nbf` des jetons
   frais est encore « dans le futur » pour lui, et aucune tolérance d'horloge n'est configurée.
3. **Preuve** : comparez l'heure des machines entre elles, puis consignez côté serveur la
   différence entre `nbf` et l'heure locale au moment du rejet : elle est positive et décroît.
4. **Prévention** : synchronisation d'horloge surveillée, tolérance explicite de quelques dizaines
   de secondes dans la validation, et un test qui rejoue la validation à la borne exacte.

## Entretien

Question posée à voix haute : *pourquoi ne lit-on jamais l'algorithme de signature dans l'en-tête
du jeton ?*

Une réponse solide énonce le principe — l'en-tête est sous le contrôle de l'émetteur du jeton, donc
de l'attaquant dans le cas hostile —, cite l'attaque `alg: none` et la confusion d'algorithmes, et
décrit la parade : une liste fermée d'algorithmes décidée par le vérificateur. Elle situe ensuite
ce contrôle dans l'ordre complet, avant la signature et bien avant toute revendication.

## Résumé

- L'ordre est forme, algorithme, signature, revendications : rien ne se décide sur du texte non
  authentifié.
- L'algorithme accepté est une décision du serveur ; l'en-tête doit s'y conformer, jamais l'inverse.
- Les signatures se comparent en temps constant, avec `CryptographicOperations.FixedTimeEquals`.
- `exp` et `nbf` se vérifient avec une tolérance d'horloge courte, qui compense la dérive sans
  étendre la durée de vie.
- `aud` et `iss` sont obligatoires : ils bloquent le rejeu d'un jeton émis pour un autre service.
- Pas de révocation possible : durée d'accès courte et jeton de rafraîchissement contrôlé en un
  point unique.

## Cartes de révision

Question : dans quel ordre un validateur enchaîne-t-il ses contrôles, et qu'est-ce qui justifie cet
ordre ? Réponse attendue : forme, algorithme, signature, puis revendications ; toute donnée lue
avant la preuve de signature peut avoir été écrite par l'attaquant.

Question : que fait un serveur correctement configuré d'un jeton dont l'en-tête annonce un
algorithme différent de celui qu'il impose ? Réponse attendue : rejet immédiat, sans vérifier la
signature annoncée ni lire les revendications — l'entrée ne choisit pas son mécanisme de
vérification.

## Test de maîtrise

Sans relire, écrivez le pseudocode complet d'un validateur : les contrôles dans l'ordre, le verdict
rendu à chaque arrêt, la tolérance d'horloge aux deux bornes temporelles, et les deux revendications
d'identité obligatoires. Justifiez ensuite, en une phrase chacune, trois décisions de cet ordre face
à l'attaque qu'elles bloquent.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
