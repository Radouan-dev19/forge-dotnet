# Documenter le contrat et le rendre vérifiable

## Objectif observable

À la fin de cette leçon, vous saurez produire un document de contrat qui décrit ce que l'API fait
réellement, distinguer un changement compatible d'une rupture, et faire échouer la construction quand
le contrat change sans que personne ne l'ait décidé.

## Prérequis

- Avoir lu `api-pagination-filtering-sorting-001` et savoir borner les paramètres d'une collection.
- Avoir lu `api-validation-problem-details-001` et savoir quelle forme prend une erreur.

## Intuition

Un document de contrat n'est utile que s'il est **dérivé du code**. Une documentation écrite à la main
diverge dès la première modification, et une documentation fausse est pire qu'absente : elle fait
perdre du temps à qui lui fait confiance.

La seconde idée est qu'un contrat publié est un engagement. Le geste professionnel n'est pas de
documenter, c'est de savoir dire si un changement casse quelqu'un — et de le prouver
automatiquement.

## Explication

**Le document décrit les points d'entrée, les formes et les statuts.** Pour chaque opération :
l'itinéraire, la méthode, les paramètres avec leurs bornes, la forme du corps attendu, et **tous** les
statuts possibles avec la forme du corps de chacun. Un document qui n'annonce que le cas passant est
incomplet là où il compte le plus : un client doit savoir à quoi ressemble une erreur pour la traiter.

**Ce que la génération automatique ne peut pas deviner.** Elle lit les types et les attributs. Elle ne
sait pas qu'une opération retourne `409` en cas de stock insuffisant, ni que `pageSize` est plafonné à
cent, ni ce que signifie `status`. Ces informations se déclarent explicitement — par des attributs de
type de réponse et des commentaires de documentation. Sans elles, le document est syntaxiquement
correct et pratiquement inutile.

**Compatible ou rupture, le critère.** Est compatible : ajouter un point d'entrée, ajouter un champ
**optionnel** en entrée, ajouter un champ en sortie, élargir un intervalle accepté. Est une rupture :
retirer ou renommer un champ, rendre obligatoire un champ qui ne l'était pas, restreindre un
intervalle, changer un type, changer le statut retourné dans un cas existant.

Le test mental : *un client écrit avant ce changement continue-t-il de fonctionner sans être
modifié ?* Si la réponse est non, c'est une rupture, et il faut la version décrite dans
`api-routing-rest-001`.

Attention au piège de l'ajout en sortie : il est compatible **si** les clients ignorent les champs
inconnus. Un client strict qui refuse tout champ non déclaré transformera votre ajout en panne. C'est
une raison de documenter la tolérance attendue.

**Le contrat devient vérifiable quand il est versionné.** Le document généré est écrit dans un fichier
suivi par le gestionnaire de versions. La construction régénère ce fichier et compare : toute
différence non commise fait échouer. Le contrat cesse alors d'être une intention et devient une
vérification, exactement comme un test.

L'effet en revue est immédiat : la modification du contrat apparaît dans le diff, lisible, et la
question « est-ce une rupture ? » se pose au bon moment — c'est le sujet de
`quality-review-diffs-001`.

**Le document est une surface publique.** Il ne contient ni point d'entrée interne, ni exemple avec
une valeur sensible, ni message d'erreur divulguant une structure de stockage. Exposer un document
complet sur un service public revient à publier la carte de sa surface d'attaque ; le restreindre aux
environnements non publics est un arbitrage légitime.

**Les exemples valent les descriptions.** Un exemple de requête et un exemple de réponse par statut
suppriment la moitié des questions. Ils doivent être exacts : un exemple qui ne passerait pas la
validation apprend au lecteur une forme fausse.

## Exemple commenté

Les déclarations qui rendent le document réellement descriptif :

```csharp
/// <summary>Crée une commande pour un client existant.</summary>
/// <response code="201">Commande créée. L'en-tête Location porte son itinéraire.</response>
/// <response code="400">Requête invalide. Le corps liste les champs fautifs.</response>
/// <response code="409">Stock insuffisant pour au moins une ligne.</response>
[HttpPost("/orders")]
[ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
public Task<IActionResult> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    => HandleAsync(request, cancellationToken);
```

Sans les trois `ProducesResponseType`, le document annoncerait un seul statut et aucune forme
d'erreur : le client ne saurait pas qu'il doit traiter le conflit.

L'extrait produit, qui est ce que lit l'appelant :

```json
{
  "/orders": {
    "post": {
      "summary": "Crée une commande pour un client existant.",
      "responses": {
        "201": { "description": "Commande créée.", "content": { "application/json": {} } },
        "400": { "description": "Requête invalide." },
        "409": { "description": "Stock insuffisant pour au moins une ligne." }
      }
    }
  }
}
```

Et la vérification qui transforme le contrat en garde-fou :

```text
# Régénère le document dans un fichier suivi, puis échoue si le fichier a changé
# sans avoir été commis. Le contrat ne peut plus bouger sans qu'on le voie.
dotnet run --project src/Api -- --emit-contract contracts/orders.json
git diff --exit-code contracts/orders.json
```

La classification d'un changement, réduite à sa règle :

```csharp
public static bool IsBreakingChange(bool removedField, bool madeRequired, bool narrowedRange)
{
    // Ajouter n'est jamais une rupture ; retirer, contraindre ou restreindre l'est toujours.
    // Le critère unique : un client écrit avant le changement fonctionne-t-il encore ?
    return removedField || madeRequired || narrowedRange;
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpPost("/orders")]
public IActionResult Create(CreateOrderRequest request)
{
    // Type de retour non typé : le document annoncera une réponse sans forme,
    // et aucun des statuts d'erreur possibles.
    ...
}
```

Accompagné, dans le dépôt, d'un fichier écrit à la main :

```text
# docs/api.md — dernière mise à jour il y a onze mois
POST /orders   -> 200 { id, total }
GET  /orders   -> 200 [ { id, total } ]
```

Trois défauts qui se cumulent.

Le document rédigé à la main n'est relié à rien. Le code retourne aujourd'hui `201` et un objet à
quatre champs ; la documentation annonce `200` et deux champs. Un client qui la suit écrira un
traitement faux, et perdra du temps à comprendre pourquoi.

Le contrôleur ne déclare ni sa forme de réponse ni ses statuts d'erreur : même une génération
automatique produirait un document muet sur `400` et `409`. Les deux cas que le client doit
absolument traiter sont ceux qui manquent.

Enfin, rien ne détecte la divergence. Aucune construction n'échoue, aucune revue ne la signale : la
documentation se périme silencieusement, et personne ne le sait avant un appel du client.

La correction : générer depuis le code, déclarer explicitement les statuts et les formes, versionner
le document produit, et faire échouer la construction sur toute différence non commise.

## Vérification de compréhension

Pour chacun de ces changements, dites s'il est compatible ou s'il constitue une rupture, et pourquoi :
ajouter un champ optionnel en entrée, renommer un champ de sortie, passer un plafond de page de cent à
cinquante.

:::quiz
id=api-openapi-contracts-001-check
question=Pourquoi versionner le document de contrat généré et faire échouer la construction sur toute différence ?
option=Parce que la génération est lente et que le fichier versionné sert de cache
option=Parce que le contrat cesse d'être une intention et devient une vérification : toute modification apparaît dans le diff et la question de la rupture se pose avant la fusion
option=Parce que les clients téléchargent le fichier directement depuis le dépôt
correct=1
success=Correct : sans comparaison automatique, la divergence est silencieuse. Avec elle, aucun changement de contrat ne peut passer inaperçu en revue.
retry=Relisez le passage sur le contrat vérifiable, et demandez-vous à quel moment on découvre la rupture dans chaque cas.
:::

## Exercice guidé

Cette leçon n'a pas d'exercice `/practice` dédié : elle se pratique sur le laboratoire
`content/labs/api-mini-erp/`, qui contient une API complète.

1. Ouvrez `content/labs/api-mini-erp/src/ForgeApiLab/Models/OrderContracts.cs` et listez, pour chaque
   opération, les statuts réellement possibles.
2. Comparez cette liste à ce que les contrôleurs déclarent. Notez chaque statut atteignable mais non
   déclaré.
3. Ajoutez les déclarations manquantes, puis relancez les tests du laboratoire pour vérifier que rien
   n'a changé du comportement.
4. Écrivez enfin, pour trois modifications de votre choix, si elles sont compatibles ou non — en
   appliquant le critère du client écrit avant le changement.

## Exercice autonome

Reprenez les points d'entrée conçus dans `api-routing-rest-001` pour la ressource « facture ».

Rédigez leur contrat complet : paramètres et bornes, forme d'entrée, forme de sortie, tous les statuts
avec le corps de chacun, un exemple exact par statut. Indiquez ensuite ce que vous versionnez, ce que
la construction vérifie, et ce que vous refusez d'exposer publiquement.

## Débogage

Un ticket indique : « Le client intégrateur dit que notre documentation ne correspond pas à
l'implémentation. »

1. **Symptôme** : divergence entre document et comportement observé.
2. **Hypothèse** : le document est rédigé à la main, ou généré sans déclaration des statuts.
3. **Preuve** : appelez l'opération dans un cas d'erreur et comparez le statut et le corps obtenus à ce
   que le document annonce.
4. **Prévention** : générer depuis le code, déclarer les statuts explicitement, versionner le document
   produit et faire échouer la construction sur toute différence.

## Entretien

Question posée à voix haute : *comment garantissez-vous que la documentation de votre API reste
exacte ?*

Une réponse solide part de la génération depuis le code, reconnaît que la génération seule ne suffit
pas parce qu'elle ignore les statuts d'erreur, et décrit la comparaison automatique du document
versionné comme la seule garantie réelle. Elle sait aussi dire qu'un document complet est une surface
publique.

## Résumé

- Un contrat écrit à la main diverge ; il doit être dérivé du code.
- La génération ignore les statuts d'erreur : ils se déclarent explicitement.
- Ajouter est compatible, retirer ou contraindre est une rupture.
- Le document versionné et comparé à la construction devient une vérification.
- Un contrat publié est une surface d'attaque : il ne contient rien d'interne.

## Cartes de révision

Question : quel test mental classe un changement en compatible ou rupture ? Réponse attendue : un
client écrit avant le changement continue-t-il de fonctionner sans être modifié ?

Question : pourquoi un ajout de champ en sortie peut-il malgré tout casser un client ? Réponse
attendue : si ce client refuse les champs qu'il ne connaît pas.

## Test de maîtrise

Sans relire, rédigez le contrat complet d'une opération « annuler une facture » : itinéraire, méthode,
paramètres et bornes, forme d'entrée, tous les statuts atteignables avec la forme de leur corps, un
exemple par statut, les déclarations à écrire dans le code, et la vérification qui empêchera ce
contrat de changer sans être vu.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
