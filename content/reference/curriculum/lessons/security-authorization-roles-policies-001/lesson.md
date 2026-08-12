# Autorisation par rôles, politiques et ressource

## Objectif observable

À la fin de cette leçon, vous saurez décider d'un droit sur **l'action et sur la ressource**, écrire
une comparaison de rôle qui ne se laisse pas tromper par une correspondance partielle, et reconnaître
la faille la plus fréquente des API : l'accès direct à un objet qui ne vous appartient pas.

## Prérequis

- Avoir lu `security-authentication-001` et savoir prouver une identité.
- Avoir lu `api-controllers-dtos-001` et savoir où vit une règle.

## Intuition

Savoir *qui* appelle ne dit pas *ce qu'il a le droit de faire*. Et savoir qu'il a le droit de modifier
« une commande » ne dit pas qu'il a le droit de modifier **cette** commande.

C'est là que se joue presque tout. Une API qui vérifie l'action mais pas la ressource laisse n'importe
quel utilisateur authentifié lire les données de tous les autres — il lui suffit de changer un
identifiant dans l'URL.

## Explication

**Deux niveaux, jamais un seul.** Le premier porte sur l'action : cet appelant peut-il, en général,
supprimer une commande ? Le second porte sur l'instance : cette commande-ci lui appartient-elle, ou
son privilège couvre-t-il celle des autres ? Le premier se déclare, le second se calcule — il faut
charger la ressource pour le trancher.

L'omission du second niveau porte un nom dans les classements de vulnérabilités d'API : c'est
l'autorisation défaillante au niveau de l'objet, et elle arrive régulièrement en tête. Elle est
d'autant plus fréquente qu'elle ne produit aucune erreur : le code fonctionne parfaitement, pour tout
le monde.

**Le rôle est une étiquette, la politique est une règle.** Un rôle — `Operator`, `Admin` — est un
attribut porté par l'identité. Une politique nomme une exigence : « être responsable de commande »
peut signifier « rôle Admin, **ou** être le propriétaire de la ressource ». La politique est le bon
niveau d'abstraction parce qu'elle survit aux réorganisations : quand les rôles changent, on modifie
la politique, pas les vingt points d'entrée qui l'utilisent.

**La comparaison de rôle est exacte, jamais partielle.** Les rôles arrivent souvent sous forme d'une
liste séparée par des virgules. Chercher une sous-chaîne dans cette liste est faux : `Admin` se trouve
dans `AdminAssistant`, et `Read` se trouve dans `Reader`. Il faut découper la liste puis comparer
chaque élément entièrement, avec une comparaison ordinale — une comparaison sensible à la culture peut
produire des équivalences inattendues.

**Le moindre privilège est la position par défaut.** Un droit se refuse tant qu'il n'est pas
explicitement accordé. La formulation inverse — tout autoriser sauf ce qui est interdit — échoue au
premier point d'entrée ajouté sans y penser. La bonne façon d'y parvenir est d'exiger une autorisation
globalement et de n'ouvrir explicitement que les points d'entrée publics.

**403 et 404, une décision de divulgation.** Répondre `403` à une ressource qui existe mais ne vous
appartient pas confirme son existence. Répondre `404` ne la confirme pas. Sur des données sensibles,
`404` est le bon choix ; sur des données banales, `403` est plus honnête et plus facile à
diagnostiquer. C'est un arbitrage explicite, déjà rencontré dans `api-http-semantics-001` — ce qu'il
ne faut pas, c'est le laisser au hasard.

**L'autorisation ne se fait pas dans l'interface.** Masquer un bouton n'est pas un contrôle d'accès :
la requête reste envoyable à la main. L'interface reflète le droit ; le serveur le fait respecter. La
même règle vaut pour un identifiant imprévisible, comme dit dans `api-routing-rest-001`.

**Ce qui doit être journalisé.** Un refus d'autorisation, avec l'identité, l'action et la ressource
visée. C'est le signal qui révèle une tentative d'accès systématique. Le journal ne contient ni la
preuve d'identité, ni le contenu de la ressource refusée.

## Exemple commenté

La comparaison exacte d'un rôle, découpée puis comparée entièrement :

```csharp
public static bool HasRole(string? declaredRoles, string requiredRole)
{
    if (string.IsNullOrWhiteSpace(declaredRoles) || string.IsNullOrWhiteSpace(requiredRole))
    {
        return false;
    }

    // Découper puis comparer chaque élément entier : une recherche de sous-chaîne
    // trouverait « Admin » dans « AdminAssistant » et accorderait un droit non détenu.
    // La comparaison est ordinale : aucune équivalence culturelle inattendue.
    return declaredRoles
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Any(role => string.Equals(role, requiredRole, StringComparison.Ordinal));
}
```

La règle de ressource, où le privilège explicite précède l'identité :

```csharp
public static bool CanEdit(string currentUserId, string resourceOwnerId, bool isAdministrator)
{
    // Le privilège explicite ouvre le droit sans regarder le propriétaire.
    if (isAdministrator)
    {
        return true;
    }

    // Sinon, l'identité doit correspondre exactement : c'est le second niveau,
    // celui dont l'oubli laisse chacun lire les données de tous les autres.
    return !string.IsNullOrWhiteSpace(currentUserId)
        && string.Equals(currentUserId, resourceOwnerId, StringComparison.Ordinal);
}
```

Et l'application des deux niveaux dans un point d'entrée :

```csharp
[HttpDelete("/orders/{id:int}")]
[Authorize(Policy = "OrderManager")]        // Premier niveau : l'action.
public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
{
    Order? order = await _orders.FindAsync(id, cancellationToken);
    if (order is null)
    {
        return NotFound();
    }

    // Second niveau : la ressource. Sans ce contrôle, tout gestionnaire de commandes
    // pourrait supprimer celles des autres en changeant l'identifiant dans l'URL.
    if (!CanEdit(User.GetUserId(), order.OwnerId, User.IsInRole("Admin")))
    {
        _logger.LogWarning(
            "Refus {UserId} sur commande {OrderId}", User.GetUserId(), id);

        // Sur une ressource sensible, NotFound plutôt que Forbid :
        // ne pas confirmer l'existence de ce qui ne vous appartient pas.
        return NotFound();
    }

    await _orders.DeleteAsync(order, cancellationToken);
    return NoContent();
}
```

## Contre-exemple et erreur fréquente

```csharp
[HttpGet("/invoices/{id:int}")]
[Authorize]                                  // Authentifié suffit : aucun droit vérifié.
public IActionResult Get(int id)
{
    // Aucun contrôle de propriété : /invoices/1 à /invoices/9999 sont tous lisibles
    // par n'importe quel utilisateur connecté.
    Invoice invoice = _invoices.Find(id);
    return Ok(invoice);
}

[HttpPost("/invoices/{id:int}/cancel")]
public IActionResult Cancel(int id)
{
    // Recherche partielle : « Admin » est trouvé dans « AdminAssistant ».
    if (!User.Claims.First(claim => claim.Type == "roles").Value.Contains("Admin"))
    {
        return Forbid();
    }

    _invoices.Cancel(id);
    return Ok();
}
```

Trois défauts, dont un critique.

Le premier point d'entrée n'exige que l'authentification. Toute personne disposant d'un compte peut
parcourir l'ensemble des factures en incrémentant un entier. C'est exactement la faille d'autorisation
au niveau de l'objet, et elle ne laisse aucune trace d'erreur : le service fonctionne comme prévu.

Le second confond appartenance à un rôle et présence d'une sous-chaîne. Un compte portant
`AdminAssistant` obtient les droits d'`Admin`. La correction est le découpage puis la comparaison
exacte.

Le troisième est plus discret : `Claims.First(...)` lève une exception si l'appelant ne porte aucun
rôle. Un défaut d'autorisation devient une erreur interne, donc un `500` là où un refus propre était
attendu.

La correction : exiger l'autorisation par défaut, vérifier la propriété après chargement, et comparer
les rôles élément par élément.

## Vérification de compréhension

Un utilisateur authentifié appelle `/invoices/842`, une facture qui appartient à un autre client.
Dites quels contrôles doivent s'exécuter, dans quel ordre, et quel statut vous retournez — en
justifiant l'arbitrage de divulgation.

:::quiz
id=security-authorization-roles-policies-001-check
question=Pourquoi vérifier l'appartenance de la ressource en plus du droit sur l'action ?
option=Parce que le droit sur l'action expire plus vite que le droit sur la ressource
option=Parce qu'un droit général autorise une catégorie d'action, pas un objet précis : sans le second contrôle, changer l'identifiant dans l'URL donne accès aux données d'autrui
option=Parce que la plateforme exige deux attributs d'autorisation par point d'entrée
correct=1
success=Correct : c'est l'autorisation défaillante au niveau de l'objet, en tête des classements de vulnérabilités d'API — et elle ne produit aucune erreur visible.
retry=Relisez le passage sur les deux niveaux, et demandez-vous ce que fait un appelant qui incrémente l'identifiant dans l'URL.
:::

## Exercice guidé

Ouvrez `security-role-check-001` dans `/practice`, puis procédez ainsi.

1. Écrivez, avant tout code, les paires qui doivent être refusées bien qu'une sous-chaîne
   corresponde.
2. Implémentez la vérification en découpant la liste puis en comparant chaque élément entier.
3. Vérifiez le cas d'une liste vide, celui d'un rôle demandé vide, et celui des blancs autour d'un
   élément.
4. Enchaînez avec `security-owner-policy-001` pour le second niveau, celui de la ressource.

## Exercice autonome

Concevez l'autorisation d'une ressource « dossier médical » lue par le patient, son médecin traitant
et un administrateur.

Décidez avant d'écrire : les rôles, les politiques et ce que chacune exige, l'ordre d'évaluation, le
statut retourné pour un accès non autorisé et la justification de cet arbitrage, ce qui est journalisé
lors d'un refus, et ce que vous répondez si la ressource n'existe pas du tout.

## Débogage

Un ticket indique : « Un client dit avoir vu la facture d'un autre client en modifiant l'adresse. »

1. **Symptôme** : une ressource est lisible par une identité qui n'y a pas droit.
2. **Hypothèse** : le point d'entrée vérifie l'authentification, pas l'appartenance de la ressource.
3. **Preuve** : appelez la même adresse avec deux comptes distincts et comparez les réponses. Une
   réponse identique confirme.
4. **Prévention** : charger puis vérifier l'appartenance avant de retourner, et ajouter un test qui
   appelle une ressource d'autrui et exige un refus.

## Entretien

Question posée à voix haute : *comment empêchez-vous un utilisateur d'accéder aux données d'un autre ?*

Une réponse solide sépare les deux niveaux, nomme la faille d'autorisation au niveau de l'objet,
explique que le second contrôle exige de charger la ressource, et traite l'arbitrage entre confirmer
et ne pas confirmer l'existence.

## Résumé

- Le droit se vérifie sur l'action **et** sur la ressource.
- Un rôle se compare entier, jamais par recherche de sous-chaîne.
- Une politique survit aux réorganisations là où un rôle codé en dur ne survit pas.
- Le refus est la position par défaut ; l'ouverture est explicite.
- Masquer un bouton n'est pas un contrôle d'accès.

## Cartes de révision

Question : pourquoi le second niveau d'autorisation ne peut-il pas être un simple attribut ? Réponse
attendue : il dépend de la ressource, donc exige de la charger avant de décider.

Question : quel statut ne confirme pas l'existence d'une ressource interdite ? Réponse attendue :
`404`, au prix d'un diagnostic plus difficile.

## Test de maîtrise

Sans relire, écrivez l'autorisation complète d'une opération « exporter les commandes d'un client » :
les deux niveaux, la politique et ce qu'elle exige, la comparaison de rôle, le statut retourné dans
chaque cas de refus avec sa justification, le contenu du journal, et les deux tests qui prouvent qu'un
appelant ne peut pas atteindre les données d'un autre.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
