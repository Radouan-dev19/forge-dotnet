# Revue de code vérifiable

Une revue de code sérieuse ne se contente pas de lister des remarques : elle range chaque remarque
par sévérité et par catégorie, et elle assume qu'une remarque cosmétique ne bloque jamais une fusion,
même quand un relecteur pressé l'a marquée en capitales. Ce projet mesure exactement cela.

Vous lisez deux diffs défectueux. Chaque défaut y est déjà étiqueté par un identifiant stable. Votre
travail est de classer chaque identifiant, et une suite d'acceptation compare votre classement au
verdict connu de chaque défaut planté.

## Contrat

Le rendu déclare `public static class Submission` et expose exactement cette méthode.

```csharp
public static string ClassifyFinding(string findingId);
```

Elle rend une chaîne de la forme `"severite:categorie"`. Les valeurs attendues sont :

- `blocking:correctness` — un défaut qui produit un résultat faux
- `blocking:security` — un défaut qui ouvre une brèche
- `blocking:concurrency` — un défaut qui casse sous accès concurrent
- `minor:style` — une remarque cosmétique, qui n'empêche jamais la fusion
- `unknown` — un identifiant qui n'appartient pas au catalogue ci-dessous

Un identifiant absent (`null`) est un appel fautif et lève `ArgumentNullException`. Un identifiant
present mais hors catalogue n'est pas fautif : il rend `unknown`.

## Diff numero 1 — le service de facturation

```diff
 public Invoice Load(string customerId)
 {
-    var customer = _repository.Find(customerId);           // [missing-null-check] Find peut rendre null
-    return customer.ToInvoice();                            // deref sans controle -> NullReference
+    var customer = _repository.Find(customerId);
+    if (customer is null) throw new InvalidOperationException();
+    return customer.ToInvoice();
 }
```

```csharp
// [sql-injection] la valeur brute est concatenee dans la requete
var query = "SELECT * FROM Orders WHERE Customer = '" + customerId + "'";
command.CommandText = query;

// [hardcoded-secret] un secret vit en clair dans la source
const string ApiKey = "FAKE-EXAMPLE-SECRET-NOT-A-REAL-KEY";
```

Trois defauts sont plantes dans ce diff : `missing-null-check`, `sql-injection` et
`hardcoded-secret`. Le premier casse la correction, les deux autres ouvrent la securite.

## Diff numero 2 — le cache partage

```csharp
// [off-by-one] la boucle deborde d'un cran et lit hors des bornes
for (int i = 0; i <= items.Length; i++)
{
    total += items[i];
}

// [unsynchronized-list-access] deux threads ecrivent la meme liste sans verrou
_pending.Add(job);

// [double-checked-locking-broken] le champ n'est pas volatile, la publication n'est pas sure
if (_instance == null)
{
    lock (_gate)
    {
        if (_instance == null) _instance = new Cache();
    }
}

// [variable-naming-nit] // BLOCKER  <- faux positif : un relecteur a marque en capitales
var x = ComputeTotal();

// [extra-blank-line] une ligne vide superflue subsiste

```

Ce diff plante `off-by-one` (correction), `unsynchronized-list-access` et
`double-checked-locking-broken` (concurrence), plus deux remarques cosmetiques :
`variable-naming-nit` et `extra-blank-line`.

## Le faux positif qui coute

La remarque `variable-naming-nit` porte un commentaire `// BLOCKER`. C'est un piege : un nommage
maladroit ne casse rien. La reponse juste est `minor:style`. La promouvoir en `blocking:*` est le
faux positif classique d'une revue, et c'est lui qui coute des points dans la suite des faux positifs.

## Catalogue de classement

| findingId | classement attendu |
| --- | --- |
| `missing-null-check` | `blocking:correctness` |
| `off-by-one` | `blocking:correctness` |
| `sql-injection` | `blocking:security` |
| `hardcoded-secret` | `blocking:security` |
| `unsynchronized-list-access` | `blocking:concurrency` |
| `double-checked-locking-broken` | `blocking:concurrency` |
| `variable-naming-nit` | `minor:style` |
| `extra-blank-line` | `minor:style` |
| tout autre identifiant non vide | `unknown` |
| `null` | leve `ArgumentNullException` |

## Ce qui est mesure

Deux suites d'acceptation, une par jalon. La suite `triage-defauts-reels` verifie que chaque defaut
reel est classe bloquant dans la bonne categorie. La suite `triage-faux-positifs` verifie que les
remarques de style restent mineures et que l'inconnu rend `unknown`. Les deux suites doivent etre
vertes pour que le projet compte comme livrable verifie.

Vous ne livrez qu'un seul fichier : `Submission.cs`. Le classement doit reposer sur une table, pas
sur une suite de branches fragiles.
