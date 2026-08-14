# Anatomie d'un jeton JWT : lisible par tous, signé par un seul

## Objectif observable

À la fin de cette leçon, vous saurez découper un jeton JWT en ses trois segments, décoder sa charge
utile à la main — alphabet Base64Url et remplissage compris —, nommer les sept revendications
normalisées les plus utiles, et expliquer à un collègue pourquoi un jeton signé n'est pas un jeton
chiffré.

## Prérequis

- Avoir lu `security-authentication-001` et savoir distinguer authentification et autorisation.
- Savoir lire un document JSON en C# avec `System.Text.Json`.

## Intuition

Un jeton JWT est une carte d'embarquement, pas une enveloppe scellée. N'importe qui peut lire le nom
du passager et le numéro de siège ; le tampon de la compagnie prouve seulement qu'elle a émis la
carte et que personne n'a modifié le siège depuis. Toute la sécurité du mécanisme tient dans ce
tampon — la signature — et rien, absolument rien, ne protège la lecture du contenu.

## Explication

**Trois segments, séparés par des points.** Un jeton JWT s'écrit
`en-tête.charge-utile.signature`. Les deux premiers segments sont du JSON encodé ; le troisième est
le résultat binaire d'un calcul cryptographique, encodé de la même façon. Découper la chaîne sur le
caractère `.` est donc la toute première opération de n'importe quel traitement : un jeton qui ne
porte pas exactement trois segments est malformé, et aucune des étapes suivantes n'a de sens sur
lui.

**Base64Url n'est pas Base64.** Un jeton voyage dans des URL et des en-têtes HTTP, où `+` et `/` ont
déjà un sens. L'encodage retenu remplace donc `+` par `-` et `/` par `_`, et supprime le remplissage
final `=`. C'est ce dernier point qui piège : `Convert.FromBase64String` exige une longueur multiple
de quatre. Avant de décoder, il faut restaurer les caractères d'origine puis rajouter le
remplissage : deux `=` si la longueur modulo quatre vaut deux, un seul si elle vaut trois. Un reste
de un est impossible en Base64 — le rencontrer signifie que le segment a été tronqué en route, par
exemple par une copie incomplète, et la seule réponse honnête est une erreur de format.

**L'en-tête décrit le traitement, pas l'identité.** Le premier segment contient typiquement deux
champs : `alg`, l'algorithme de signature annoncé, et `typ`, le type de jeton. Retenez dès
maintenant une règle qui sera au centre de la leçon suivante : ce segment est écrit par l'émetteur,
mais rien n'empêche un attaquant de le réécrire. Tout ce que l'en-tête annonce doit être confronté à
ce que le serveur impose, jamais cru sur parole.

**La charge utile porte des revendications.** Le deuxième segment est un objet JSON dont chaque
propriété est une *revendication* (claim) : une affirmation de l'émetteur au sujet du porteur. Sept
sont normalisées et reviennent partout. `iss` (issuer) identifie l'émetteur. `sub` (subject)
identifie le porteur — un identifiant d'utilisateur, pas son nom d'affichage. `aud` (audience) nomme
le ou les destinataires prévus : une API qui accepte un jeton émis pour une autre ouvre la porte au
rejeu croisé. `exp` (expiration) et `nbf` (not before) bornent la fenêtre de validité en secondes
d'époque Unix. `iat` (issued at) date l'émission. `jti` (JWT id) donne un identifiant unique au
jeton, utile pour tracer ou dédoublonner. Tout le reste est libre : rôles, portées, locataire — ce
que l'émetteur veut affirmer.

**La signature prouve l'origine et l'intégrité, rien d'autre.** Le troisième segment est, pour
l'algorithme HMAC-SHA256, le condensat calculé sur la chaîne `en-tête.charge-utile` — les deux
segments encodés, point compris — avec une clé secrète partagée entre l'émetteur et le vérificateur.
Quiconque détient la clé peut recalculer ce condensat et le comparer : s'ils diffèrent, le jeton a
été modifié ou émis par quelqu'un d'autre. C'est une preuve d'origine et d'intégrité. Ce n'est pas
une protection de confidentialité.

**Le point que presque personne ne dit à voix haute : un JWT n'est pas chiffré.** Décoder Base64Url
ne demande aucune clé, aucun secret, aucun calcul difficile. Toute personne qui intercepte un jeton
— dans un journal, un rapport d'erreur, un historique de navigateur — lit sa charge utile en
entier, en trois lignes de code ou en collant la chaîne dans n'importe quel décodeur public.
Signer, c'est apposer un tampon vérifiable sur un texte en clair ; chiffrer, c'est rendre le texte
illisible sans clé. Un JWT standard fait le premier, jamais le second. La conséquence pratique est
une règle absolue : aucune donnée confidentielle dans une charge utile — pas de mot de passe, pas de
numéro personnel, pas de secret d'API. La charge utile est une pièce d'identité publique, pas un
coffre.

**Ce que cette structure achète, et ce qu'elle coûte.** Parce que tout est dans le jeton, le serveur
qui le vérifie n'a besoin d'aucun état : pas de session en base, pas d'appel à l'émetteur à chaque
requête. C'est ce qui rend le mécanisme si répandu dans les API. Le coût symétrique est qu'un jeton
émis vit sa vie : le serveur ne peut pas le rappeler, et chaque octet de revendication voyage dans
chaque requête. Ces deux limites structurent la leçon suivante, consacrée à la validation.

## Exemple commenté

Décoder la charge utile à la main, sans bibliothèque dédiée — c'est exactement ce que fait une
bibliothèque de production avant validation :

```csharp
using System.Text.Json;

public static JsonDocument ReadPayload(string token)
{
    // Trois segments, ni plus ni moins : tout autre découpage est un jeton malformé.
    string[] segments = token.Split('.');
    if (segments.Length != 3)
    {
        throw new FormatException("Un jeton JWT porte exactement trois segments.");
    }

    // Restaurer l'alphabet Base64 classique à partir de Base64Url.
    string payload = segments[1].Replace('-', '+').Replace('_', '/');

    // Restaurer le remplissage supprimé à l'encodage : la longueur doit être multiple de quatre.
    int remainder = payload.Length % 4;
    if (remainder == 1)
    {
        // Aucune chaîne Base64 valide n'a un reste de un : le segment a été tronqué.
        throw new FormatException("Segment Base64Url tronqué.");
    }

    if (remainder == 2)
    {
        payload += "==";
    }
    else if (remainder == 3)
    {
        payload += "=";
    }

    byte[] bytes = Convert.FromBase64String(payload);
    return JsonDocument.Parse(bytes);
}
```

Lire ensuite une revendication est du JSON ordinaire :

```csharp
using JsonDocument payload = ReadPayload(token);
// La revendication d'audience peut manquer : TryGetProperty évite l'exception de clé absente.
if (payload.RootElement.TryGetProperty("aud", out JsonElement audience))
{
    Console.WriteLine($"Jeton émis pour : {audience}");
}
```

## Contre-exemple et erreur fréquente

Le code fautif ci-dessous émet un jeton en croyant sa charge utile confidentielle :

```csharp
// FAUTIF : ces revendications sont lisibles par quiconque voit passer le jeton.
var claims = new Dictionary<string, string>
{
    ["sub"] = user.Id,
    ["email"] = user.Email,                  // Donnée personnelle en clair.
    ["reset_answer"] = user.SecurityAnswer,  // Secret de récupération en clair !
};
string token = IssueSignedToken(claims, signingKey);
```

Le symptôme n'apparaît jamais en développement : tout fonctionne, la signature est valide, les
tests passent. Il apparaît le jour où un jeton fuit — un journal d'accès, une URL copiée dans un
ticket — et où quelqu'un colle la chaîne dans un décodeur : la réponse de sécurité du compte est
en clair dans le deuxième segment. La signature n'y change rien, puisqu'elle ne cache rien.

La correction ne passe pas par un encodage plus malin : elle consiste à ne mettre dans la charge
utile que des identifiants opaques et des droits, et à garder toute donnée sensible côté serveur,
consultée à partir de `sub` :

```csharp
// CORRIGÉ : le jeton ne porte que l'identifiant et les droits ; le reste demeure en base.
var claims = new Dictionary<string, string>
{
    ["sub"] = user.Id,
    ["scope"] = "orders.read",
};
```

## Vérification de compréhension

Avant le quiz, répondez à voix haute : que faut-il posséder pour lire la charge utile d'un JWT
intercepté, et que faut-il posséder pour la modifier sans être détecté ?

:::quiz
id=security-jwt-anatomy-001-check
question=Un jeton JWT signé en HMAC-SHA256 transite dans un journal accessible à toute l'équipe. Que peut lire une personne sans la clé secrète ?
option=Rien : sans la clé, les segments restent illisibles
option=L'en-tête seulement, la charge utile étant protégée par la signature
option=L'en-tête et la charge utile en entier : seule la falsification est détectable, pas la lecture
correct=2
success=Exact : Base64Url se décode sans aucun secret. La clé ne sert qu'à produire et vérifier la signature, jamais à cacher le contenu.
retry=Relisez la distinction entre signer et chiffrer : laquelle des deux opérations exige une clé pour LIRE ?
:::

## Exercice guidé

Ouvrez l'exercice `security-jwt-decode-001` dans `/practice`, puis procédez ainsi.

1. Écrivez d'abord le découpage en segments et le refus des jetons qui n'en portent pas trois.
2. Implémentez la restauration Base64Url : remplacement des deux caractères, puis remplissage
   calculé sur la longueur modulo quatre.
3. Analysez le JSON obtenu et retournez la revendication demandée, en décidant explicitement du
   comportement quand elle est absente.
4. Avant de soumettre, prédisez sur papier la sortie de chaque cas visible.

## Exercice autonome

Écrivez une méthode qui reçoit un jeton et retourne la liste des noms de revendications présentes
dans sa charge utile, triée par ordre alphabétique. Écrivez avant le code vos hypothèses : que
faire d'un segment vide, d'un JSON dont la racine n'est pas un objet, d'une charge utile sans
aucune revendication ? Vérifiez ensuite chaque hypothèse avec un jeton fabriqué à la main.

## Débogage

Un ticket indique : « Le décodage des jetons échoue avec `FormatException: The input is not a
valid Base-64 string` — mais seulement pour certains utilisateurs, et jamais en recette. »

1. **Symptôme** : l'exception survient sur une partie des jetons seulement, les autres se décodent.
2. **Hypothèse** : le code appelle `Convert.FromBase64String` sur le segment brut ; l'échec ne
   touche que les charges utiles dont l'encodage contient `-` ou `_`, ou dont la longueur exige un
   remplissage. Les jetons courts de recette n'en contenaient pas, par pur hasard d'encodage.
3. **Preuve** : consignez la longueur modulo quatre et la présence des deux caractères Base64Url
   pour un jeton en échec ; comparez avec un jeton qui passe.
4. **Prévention** : centraliser le décodage Base64Url dans une méthode unique testée avec des
   longueurs de reste zéro, deux et trois — et un cas tronqué de reste un.

## Entretien

Question posée à voix haute : *que contient un JWT, et qui peut le lire ?*

Une réponse solide décrit les trois segments et leur rôle, affirme sans détour que l'en-tête et la
charge utile sont lisibles par quiconque détient la chaîne, et réserve à la signature le rôle de
preuve d'origine et d'intégrité. Elle en tire la conséquence opérationnelle — aucune donnée
confidentielle dans les revendications — et cite deux ou trois revendications normalisées en
expliquant à quoi chacune sert.

## Résumé

- Un jeton JWT porte trois segments : en-tête, charge utile, signature, séparés par des points.
- Base64Url remplace `+` et `/` et supprime le remplissage : le décodage doit le restaurer.
- Les revendications normalisées — `iss`, `sub`, `aud`, `exp`, `nbf`, `iat`, `jti` — décrivent
  l'émetteur, le porteur, les destinataires et la fenêtre de validité.
- La signature prouve l'origine et l'intégrité ; elle ne cache rien.
- Aucune donnée confidentielle ne va dans une charge utile : elle est publique par construction.

## Cartes de révision

Question : pourquoi ajouter `==` avant `Convert.FromBase64String` sur un segment de jeton ?
Réponse attendue : Base64Url supprime le remplissage à l'émission ; le décodeur Base64 classique
exige une longueur multiple de quatre, il faut donc le restaurer selon la longueur modulo quatre.

Question : que garantit la signature HMAC d'un jeton, et que ne garantit-elle pas ?
Réponse attendue : elle garantit que le jeton vient d'un détenteur de la clé et n'a pas été
modifié ; elle ne garantit aucune confidentialité, la charge utile restant lisible par tous.

## Test de maîtrise

Sans relire, écrivez la méthode qui décode la charge utile d'un jeton et retourne la valeur de
`sub`, en refusant proprement un jeton à deux segments et un segment tronqué. Expliquez ensuite en
trois phrases à un collègue non technique pourquoi le contenu du jeton n'est pas secret.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
