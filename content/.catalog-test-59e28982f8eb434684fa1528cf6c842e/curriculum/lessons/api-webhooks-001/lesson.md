# Webhooks : signer la charge utile, dater l'envoi, fermer la fenêtre de rejeu

## Objectif observable

À la fin de cette leçon, vous saurez vérifier qu'un webhook reçu vient bien de l'émetteur attendu
par une signature HMAC de sa charge utile, et refuser un envoi rejoué en confrontant son
horodatage à une fenêtre de tolérance — deux contrôles distincts qui, ensemble, font un point de
réception digne de confiance.

## Prérequis

- Avoir lu `api-http-semantics-001` : le webhook est un POST entrant que vous ne sollicitez pas.
- Avoir lu `security-jwt-signature-001` : la vérification HMAC et la comparaison en temps constant.

## Intuition

Un webhook renverse le sens habituel : ce n'est pas vous qui appelez, c'est un service tiers qui
vous appelle quand un événement survient. Vous exposez une adresse, et n'importe qui peut y poster.
Deux questions se posent donc à chaque réception, distinctes : « cet envoi vient-il vraiment de
l'émetteur attendu ? » — la signature — et « est-ce un envoi frais, ou la copie d'un ancien
rejouée contre moi ? » — l'horodatage. Ni l'une ni l'autre seule ne suffit.

## Explication

**La signature HMAC prouve l'origine et l'intégrité.** L'émetteur et vous partagez un secret.
L'émetteur calcule le condensat HMAC de la charge utile — le corps exact, octet pour octet — avec
ce secret, et le joint dans un en-tête. À la réception, vous recalculez le même condensat sur le
corps reçu et comparez : s'ils coïncident, l'envoi vient d'un détenteur du secret et n'a pas été
modifié en route. C'est exactement la vérification de signature de jeton vue au prompt sur les
jetons — même HMAC, même comparaison en temps constant, même refus silencieux d'un corps qui ne
correspond pas. La seule différence est ce qu'on signe : ici, le corps de la requête, pas les
segments d'un jeton.

**Signer le corps *brut*, avant toute interprétation.** Le condensat doit porter sur les octets
reçus tels quels — pas sur le résultat d'une désérialisation puis d'une re-sérialisation, qui
réordonnerait les clés ou normaliserait les espaces et produirait un corps *différent* de celui
signé. C'est le piège symétrique de l'ETag instable : là on voulait une empreinte stable d'un
même contenu, ici on veut vérifier une empreinte sur le contenu *exact*, et toute reconstruction
la casse. On lit donc le corps brut, on vérifie, et seulement ensuite on désérialise.

**L'horodatage ferme la fenêtre de rejeu.** La signature seule ne protège pas du rejeu : un envoi
authentique capturé — dans un journal, un intermédiaire — peut être renvoyé tel quel, signature
comprise, et il sera valide indéfiniment. La parade est de faire *signer aussi un horodatage* —
inclus dans ce que couvre le HMAC, donc infalsifiable — et de refuser à la réception tout envoi
dont l'horodatage s'écarte trop de l'instant courant. Un envoi vieux de plusieurs minutes est
rejeté même si sa signature est parfaite : la fenêtre étroite rend le rejeu impraticable, car le
temps de capturer et de renvoyer, l'envoi est déjà périmé.

**La tolérance d'horloge, encore.** Comme pour la validité des jetons, l'émetteur et vous n'avez
pas exactement la même heure. La fenêtre accepte donc un écart borné de part et d'autre de
l'instant courant — quelques minutes, pas quelques heures. Trop étroite, elle rejette des envois
légitimes quand les horloges dérivent ou que le réseau retarde ; trop large, elle rouvre le rejeu
qu'elle devait fermer. C'est le même arbitrage que la tolérance des jetons, transposé au sens de
la réception.

**Rejeu vraiment fermé : signature *plus* horodatage, et idéalement un identifiant unique.** Les
deux contrôles se complètent : la signature dit *qui*, l'horodatage dit *quand*, et refuser
l'ancien élimine la fenêtre où un envoi authentique redevient une arme. Les émetteurs sérieux
ajoutent un identifiant d'envoi unique que le récepteur mémorise le temps de la fenêtre, pour
rejeter même un rejeu immédiat ; l'horodatage seul suffit déjà à rendre le rejeu à froid
impraticable, et c'est le cœur de cette leçon.

## Exemple commenté

Vérifier un webhook — signature sur le corps horodaté, puis fenêtre — noyau des exercices :

```csharp
using System.Security.Cryptography;
using System.Text;

// La signature couvre l'horodatage ET le corps : on la recalcule sur la même chaîne.
public static bool HasValidSignature(long timestamp, string rawBody, string secret, string presented)
{
    string signedContent = timestamp + "." + rawBody;
    byte[] expected = HMACSHA256.HashData(
        Encoding.UTF8.GetBytes(secret),
        Encoding.UTF8.GetBytes(signedContent));
    byte[] given = Convert.FromHexString(presented);

    // Temps constant : la durée de comparaison ne révèle pas le point de divergence.
    return given.Length == expected.Length
        && CryptographicOperations.FixedTimeEquals(expected, given);
}

// L'horodatage doit tomber dans la fenêtre autour de l'instant courant, tolérance comprise.
public static bool IsWithinReplayWindow(long timestamp, long now, int toleranceSeconds)
{
    long drift = Math.Abs(now - timestamp);
    return drift <= toleranceSeconds;
}
```

La signature porte sur `horodatage.corps` : lier les deux empêche de recoller un vieux corps signé
à un horodatage frais, ou l'inverse.

## Contre-exemple et erreur fréquente

Le code fautif re-sérialise le corps avant de vérifier la signature :

```csharp
// FAUTIF : on désérialise, puis on re-sérialise pour vérifier — le corps a changé de forme.
var payload = JsonSerializer.Deserialize<Event>(rawBody);
string reserialized = JsonSerializer.Serialize(payload);   // Clés réordonnées, espaces perdus.
bool ok = HasValidSignature(timestamp, reserialized, secret, presented);   // Échoue toujours.
```

Le symptôme : *toutes* les signatures échouent, même les authentiques, car le corps re-sérialisé
ne coïncide jamais octet pour octet avec celui que l'émetteur a signé. La correction vérifie sur le
brut, et ne désérialise qu'après :

```csharp
// CORRIGÉ : vérifier d'abord sur le corps brut reçu, désérialiser ensuite.
if (!HasValidSignature(timestamp, rawBody, secret, presented))
{
    return Unauthorized();
}
var payload = JsonSerializer.Deserialize<Event>(rawBody);
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : un webhook parfaitement signé arrive avec un horodatage
d'il y a une heure. L'acceptez-vous ? Qu'est-ce que ce refus protège que la signature ne protège
pas ?

:::quiz
id=api-webhooks-001-check
question=Pourquoi vérifier la signature d'un webhook ne suffit-il pas, et que faut-il ajouter ?
option=Rien : une signature HMAC valide garantit à elle seule que l'envoi est légitime et frais
option=Il faut aussi vérifier un horodatage signé contre une fenêtre étroite, car un envoi authentique capturé peut être rejoué tel quel, signature comprise
option=Il faut chiffrer le corps, la signature ne protégeant pas la confidentialité
correct=1
success=Exact : la signature dit qui et garantit l'intégrité, mais ne dit pas quand. Sans fenêtre d'horodatage, un envoi authentique rejoué reste valide indéfiniment.
retry=Distinguez ce que prouve la signature — origine et intégrité — de ce qu'elle ne dit pas : la fraîcheur. Qu'est-ce qui date l'envoi ?
:::

## Exercice guidé

Ouvrez l'exercice `api-webhook-signature-001` dans `/practice`, puis procédez ainsi.

1. Reconstituez la chaîne signée : l'horodatage et le corps, joints comme le contrat le prescrit.
2. Recalculez le condensat HMAC et comparez en temps constant, en refusant toute longueur
   incohérente.
3. Ne touchez pas au corps avant la vérification : il se compare tel qu'il est reçu.
4. Prédisez le verdict de chaque cas, dont un corps altéré d'un caractère.

## Exercice autonome

Décrivez le point de réception d'un webhook de paiement : quels en-têtes vous lisez, dans quel
ordre vous vérifiez signature et horodatage, ce que vous faites d'un rejeu détecté, et quelle
fenêtre de tolérance vous choisissez — avec sa justification.

## Débogage

Un ticket indique : « L'intégration du fournisseur de paiement échoue : toutes ses notifications
sont rejetées comme non signées, alors que le secret est le bon et copié sans erreur. »

1. **Symptôme** : rejet systématique pour signature invalide, secret pourtant correct.
2. **Hypothèse** : la signature est vérifiée sur un corps re-sérialisé, ou sur le corps seul sans
   l'horodatage que l'émetteur inclut dans la chaîne signée.
3. **Preuve** : comparer octet pour octet le corps brut reçu et la chaîne effectivement passée au
   HMAC ; vérifier la présence de l'horodatage dans cette chaîne.
4. **Prévention** : lire et conserver le corps brut avant toute désérialisation, et reconstituer
   la chaîne signée exactement comme la documentation de l'émetteur la définit.

## Entretien

Question posée à voix haute : *comment sécurisez-vous un point de réception de webhooks ?*

Une réponse solide sépare les deux contrôles — signature HMAC pour l'origine et l'intégrité,
horodatage dans une fenêtre étroite pour la fraîcheur — et explique pourquoi la signature seule
laisse la porte au rejeu. Elle insiste sur la vérification du corps brut avant désérialisation, la
comparaison en temps constant, la tolérance d'horloge, et cite l'identifiant d'envoi unique comme
renfort. Le lien avec la vérification de signature de jeton montre l'unité du sujet.

## Résumé

- La signature HMAC du corps prouve l'origine et l'intégrité — même mécanisme que la signature de
  jeton.
- Vérifier sur le corps *brut*, avant toute désérialisation, sinon la signature échoue toujours.
- La signature ne dit pas *quand* : un envoi authentique se rejoue indéfiniment sans autre
  contrôle.
- Un horodatage signé, confronté à une fenêtre étroite avec tolérance d'horloge, ferme le rejeu.
- Comparaison en temps constant, et identifiant d'envoi unique en renfort contre le rejeu
  immédiat.

## Cartes de révision

Question : pourquoi faut-il vérifier la signature d'un webhook sur le corps brut plutôt que sur
une version re-sérialisée ? Réponse attendue : la re-sérialisation réordonne les clés et
normalise les espaces, produisant un corps différent de celui signé, si bien que la signature
échoue même quand l'envoi est authentique.

Question : que protège la fenêtre d'horodatage que la signature ne protège pas ? Réponse
attendue : le rejeu — un envoi authentique capturé et renvoyé reste valide sans elle ; la fenêtre
étroite le rend périmé avant qu'il ne puisse être rejoué à froid.

## Test de maîtrise

Sans relire, écrivez la procédure complète de réception d'un webhook, dans l'ordre — lecture du
corps brut, reconstitution de la chaîne signée, vérification HMAC en temps constant, contrôle
d'horodatage, puis désérialisation — et justifiez chaque étape par l'attaque qu'elle bloque ou le
piège qu'elle évite.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
