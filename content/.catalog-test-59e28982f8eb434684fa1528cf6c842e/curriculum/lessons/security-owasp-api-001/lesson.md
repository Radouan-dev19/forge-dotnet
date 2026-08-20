# Socle OWASP pour une API locale

## Objectif observable

À la fin de cette leçon, vous saurez appliquer une liste de contrôles de sécurité à une API que vous
écrivez, reconnaître les quatre risques qui reviennent le plus souvent, et justifier chaque contrôle
par la menace précise qu'il ferme.

## Prérequis

- Avoir lu `security-authorization-roles-policies-001` et savoir vérifier un droit sur une ressource.
- Avoir lu `api-pagination-filtering-sorting-001` et savoir borner une entrée.

## Intuition

Une liste de risques n'a d'intérêt que si chaque ligne se traduit par un geste dans votre code. Retenir
« il faut valider les entrées » ne change rien ; savoir *quelle* entrée, *quelle* borne et *quel test
le prouve* change tout.

Le fil conducteur : la sécurité d'une API n'est pas une couche ajoutée à la fin, c'est une série de
décisions prises aux mêmes endroits que les décisions fonctionnelles.

## Explication

**Risque 1 — accès à un objet qui ne vous appartient pas.** C'est le plus fréquent. Il a été traité en
détail dans `security-authorization-roles-policies-001` : vérifier le droit sur la ressource, pas
seulement sur l'action. Le contrôle qui le ferme est un test qui appelle la ressource d'autrui et
exige un refus.

**Risque 2 — authentification faible.** Message d'échec révélateur, comparaison à durée variable,
absence de limitation du nombre d'essais. Le troisième point n'a pas encore été traité : sans
limitation, un attaquant essaie des milliers de mots de passe par minute. La limitation se fait par
compte **et** par origine, avec un délai croissant plutôt qu'un blocage définitif — sans quoi
verrouiller un compte devient soi-même une attaque.

**Risque 3 — exposition excessive de données.** Renvoyer l'entité entière et laisser l'interface
n'afficher que trois champs publie tout le reste. C'est le raisonnement de `api-controllers-dtos-001` :
la réponse contient ce que vous avez décidé de publier, champ par champ.

Sa variante d'entrée est la sur-affectation : un champ non prévu du corps qui atteint l'entité. Le
contrôle qui la ferme est un test envoyant un champ interdit et vérifiant qu'il est ignoré.

**Risque 4 — absence de bornes.** Taille de page, taille de corps, longueur de chaîne, nombre
d'éléments d'une liste, profondeur d'un objet imbriqué. Chacune non bornée est un moyen de consommer
vos ressources avec une seule requête. La borne se pose côté serveur, jamais côté client.

**Injection, toujours d'actualité.** Concaténer une valeur reçue dans une requête est la forme
classique ; concaténer un **nom de colonne** en est la forme oubliée, vue dans
`api-pagination-filtering-sorting-001`. La règle : les valeurs se paramètrent, les identifiants se
choisissent dans une liste fermée.

**Mauvaise configuration.** Un document de contrat exposé publiquement, une page d'erreur détaillée
laissée active en production, un point d'entrée de diagnostic accessible, une origine croisée ouverte
à tous. Chacune est une décision par défaut que personne n'a prise consciemment.

L'origine croisée mérite une mention : autoriser toutes les origines avec envoi d'identifiants est
refusé par les navigateurs, précisément parce que c'est dangereux. Une liste d'origines nommées est la
seule forme acceptable.

**Redirection ouverte.** Rediriger vers une destination fournie par l'appelant permet d'utiliser votre
domaine — donc votre crédibilité — pour envoyer quelqu'un ailleurs. Seul un chemin local est
acceptable, comme vu dans `security-authentication-001`.

**Journalisation et surveillance insuffisantes.** Un incident non détecté dure. Les refus
d'autorisation, les échecs de connexion répétés et les erreurs internes doivent être visibles. Et le
journal lui-même ne contient ni secret, ni donnée personnelle inutile.

**Dépendances non tenues à jour.** Une bibliothèque vulnérable est une faille que vous n'avez pas
écrite mais que vous exposez. La vérification s'automatise dans la chaîne de construction, sujet de
`ci-pipeline-build-test-001`.

## Exemple commenté

Le refus de redirection externe, dans sa forme complète :

```csharp
public static bool IsLocalRedirect(string? target)
{
    if (string.IsNullOrWhiteSpace(target))
    {
        return false;
    }

    string candidate = target.Trim();

    // Une barre unique en tête désigne un chemin local. Deux barres désignent un autre
    // hôte — c'est une redirection externe déguisée. Une barre inversée est traitée
    // comme une barre par certains clients : elle est refusée pour la même raison.
    if (!candidate.StartsWith('/') || candidate.StartsWith("//", StringComparison.Ordinal))
    {
        return false;
    }

    return !candidate.StartsWith("/\\", StringComparison.Ordinal);
}
```

Les bornes d'entrée, posées à un seul endroit :

```csharp
// Chaque borne ferme une menace nommée : un corps illimité sature la mémoire,
// une chaîne illimitée sature le stockage, une liste illimitée sature le traitement.
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2 * 1024 * 1024;
});

builder.Services.AddCors(options => options.AddPolicy("interne", policy => policy
    // Origines nommées, pas de joker : un joker avec identifiants est refusé
    // par les navigateurs, et sans identifiants il ouvre l'API à tout site.
    .WithOrigins("https://portail.interne.local")
    .WithMethods("GET", "POST")
    .AllowCredentials()));
```

Et la limitation d'essais, qui manque le plus souvent :

```csharp
public static TimeSpan RetryDelay(int failedAttempts)
{
    // Délai croissant plutôt que verrouillage : bloquer définitivement un compte
    // permettrait à un tiers de priver son titulaire d'accès à volonté.
    if (failedAttempts <= 3)
    {
        return TimeSpan.Zero;
    }

    int seconds = Math.Min(300, (int)Math.Pow(2, failedAttempts - 3));
    return TimeSpan.FromSeconds(seconds);
}
```

## Contre-exemple et erreur fréquente

```csharp
var builder = WebApplication.CreateBuilder(args);

// Toutes les origines, toutes les méthodes, tous les en-têtes : l'API devient
// appelable depuis n'importe quel site visité par l'utilisateur.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// Page d'erreur détaillée active partout : la pile, les chemins et les versions
// des bibliothèques sont publiés au premier défaut.
app.UseDeveloperExceptionPage();

app.MapGet("/debug/config", (IConfiguration configuration) =>
    // Point d'entrée de diagnostic, sans autorisation, qui publie la configuration
    // entière — chaînes de connexion comprises.
    configuration.AsEnumerable());

app.MapGet("/search", (string term) =>
    // Aucune borne, aucune paramétrisation : injection et balayage complet.
    db.Query($"SELECT * FROM Products WHERE Name LIKE '%{term}%'"));
```

Quatre décisions par défaut, quatre failles.

L'origine croisée ouverte rend l'API appelable depuis n'importe quel site que visite un utilisateur
authentifié. Le navigateur enverra ses jetons de session avec la requête.

La page d'erreur détaillée publie exactement ce que `api-validation-problem-details-001` interdit :
pile, chemins de fichiers, versions. C'est une carte de la surface d'attaque, offerte au premier
défaut déclenché.

`/debug/config` est le plus direct : sans autorisation, il retourne toute la configuration, secrets
compris. Ce genre de point d'entrée est presque toujours ajouté « temporairement ».

La recherche concatène le terme reçu, ce qui est une injection, et ne borne ni la longueur du terme ni
le nombre de résultats.

## Vérification de compréhension

Pour chacun de ces trois éléments, nommez la menace qu'il ferme : une taille de corps maximale, une
liste d'origines nommées, un test qui appelle la ressource d'un autre utilisateur.

:::quiz
id=security-owasp-api-001-check
question=Pourquoi un délai croissant est-il préférable à un verrouillage définitif après plusieurs échecs de connexion ?
option=Parce que le verrouillage définitif est plus coûteux à implémenter
option=Parce qu'un verrouillage déclenchable par un tiers permet de priver un titulaire légitime de son accès : le blocage devient lui-même une attaque
option=Parce que le délai croissant permet de continuer à mesurer le temps de réponse
correct=1
success=Correct : un contrôle de sécurité qui peut être retourné contre l'utilisateur légitime crée une nouvelle vulnérabilité.
retry=Relisez le passage sur la limitation d'essais, et demandez-vous qui peut déclencher le verrouillage d'un compte.
:::

## Exercice guidé

Ouvrez `security-local-redirect-001` dans `/practice`, puis procédez ainsi.

1. Listez, avant tout code, les formes à refuser : URL absolue, double barre, barre inversée, valeur
   absente, blancs de bordure.
2. Implémentez la vérification en n'acceptant qu'un chemin local à barre unique.
3. Vérifiez chaque forme refusée séparément, avec un cas par menace.
4. Reprenez ensuite `security-owner-policy-001` et vérifiez que votre implémentation refuse bien
   l'accès croisé.

## Exercice autonome

Auditez une API que vous avez écrite, ou celle du laboratoire `content/labs/api-mini-erp/`.

Pour chacun des risques de cette leçon, écrivez : le contrôle correspondant est-il présent, où
exactement dans le code, et quel test le prouve. Là où le contrôle manque, écrivez la menace précise
qu'ouvre son absence — pas une formule générale.

## Débogage

Un ticket indique : « Un utilisateur signale que notre API répond à des requêtes venant d'un autre
site. »

1. **Symptôme** : une origine tierce obtient une réponse au lieu d'un refus du navigateur.
2. **Hypothèse** : la politique d'origine croisée autorise toutes les origines.
3. **Preuve** : inspectez l'en-tête d'autorisation d'origine dans la réponse. Un joker confirme.
4. **Prévention** : remplacer par une liste d'origines nommées, et ajouter un test qui vérifie qu'une
   origine inconnue n'obtient pas d'en-tête permissif.

## Entretien

Question posée à voix haute : *quels sont les risques principaux d'une API, et comment les traitez-vous
concrètement ?*

Une réponse solide cite trois ou quatre risques avec, pour chacun, le geste précis dans le code et le
test qui le prouve. Elle place l'autorisation au niveau de l'objet en tête, et évite les formules
générales sans traduction technique.

## Résumé

- L'accès à l'objet d'autrui est le risque le plus fréquent, et le plus silencieux.
- Toute entrée non bornée est un moyen de consommer vos ressources.
- Les valeurs se paramètrent, les identifiants se choisissent dans une liste fermée.
- Les mauvaises configurations sont des décisions par défaut que personne n'a prises.
- Un contrôle de sécurité retournable contre l'utilisateur légitime est une faille de plus.

## Cartes de révision

Question : quelle est la forme oubliée de l'injection ? Réponse attendue : la concaténation d'un nom
de colonne, qu'une requête paramétrée ne protège pas.

Question : pourquoi une origine croisée ouverte est-elle dangereuse pour un utilisateur connecté ?
Réponse attendue : le navigateur joint ses jetons de session à la requête émise depuis le site tiers.

## Test de maîtrise

Sans relire, listez huit contrôles de sécurité d'une API. Pour chacun : la menace fermée, l'endroit
exact du code où il se pose, et le test qui prouve qu'il est actif. Terminez en désignant celui dont
l'absence serait la plus grave et en justifiant ce classement.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
