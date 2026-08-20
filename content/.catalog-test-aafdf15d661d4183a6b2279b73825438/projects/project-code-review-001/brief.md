# Revue de code vérifiable

Une revue de code sérieuse ne se contente pas de lister des remarques : elle range chaque remarque
par sévérité et par catégorie, et elle assume qu'une remarque cosmétique ne bloque jamais une fusion,
même quand un relecteur pressé l'a marquée en capitales. Ce projet mesure exactement cela.

Vous lisez quatre diffs défectueux. Chaque défaut y est déjà étiqueté par un identifiant stable. Votre
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

## Diff numero 3 — la file des paiements

```csharp
// [check-then-act-race] le controle et l'action ne sont pas atomiques : deux threads passent le guichet
if (!_processed.Contains(paymentId))
{
    _processed.Add(paymentId);
    Charge(paymentId);
}

// [weak-random-token] un jeton de session sort d'un generateur previsible
var token = new Random().Next().ToString();

// [format-preference-nit] // BLOCKER  <- un relecteur prefere l'autre style d'accolades
if (isRetry) { Requeue(paymentId); }
```

Ce diff plante `check-then-act-race` (concurrence) et `weak-random-token` (securite), plus une
preference de mise en forme marquee en capitales : `format-preference-nit`.

## Diff numero 4 — l'export de rapports

```csharp
// [non-atomic-increment] l'increment lu-modifie-ecrit perd des exports sous acces concurrent
_exportCount++;

// [path-traversal] le nom de fichier fourni par le client rejoint le chemin sans nettoyage
var path = Path.Combine(_exportRoot, request.FileName);
File.WriteAllText(path, report);
```

Ce diff plante `non-atomic-increment` (concurrence) et `path-traversal` (securite).

## Le faux positif qui coute

La remarque `variable-naming-nit` porte un commentaire `// BLOCKER`. C'est un piege : un nommage
maladroit ne casse rien. La reponse juste est `minor:style`. La promouvoir en `blocking:*` est le
faux positif classique d'une revue, et c'est lui qui coute des points dans la suite des faux positifs.
Le diff numero 3 tend le meme piege une seconde fois avec `format-preference-nit` : la capitale du
commentaire n'anoblit pas une querelle d'accolades, qui reste cosmetique quoi qu'en dise son auteur.

## Catalogue de classement

| findingId | classement attendu |
| --- | --- |
| `missing-null-check` | `blocking:correctness` |
| `off-by-one` | `blocking:correctness` |
| `sql-injection` | `blocking:security` |
| `hardcoded-secret` | `blocking:security` |
| `weak-random-token` | `blocking:security` |
| `path-traversal` | `blocking:security` |
| `unsynchronized-list-access` | `blocking:concurrency` |
| `double-checked-locking-broken` | `blocking:concurrency` |
| `check-then-act-race` | `blocking:concurrency` |
| `non-atomic-increment` | `blocking:concurrency` |
| `variable-naming-nit` | `minor:style` |
| `extra-blank-line` | `minor:style` |
| `format-preference-nit` | `minor:style` |
| tout autre identifiant non vide | `unknown` |
| `null` | leve `ArgumentNullException` |

## Ce qui est mesure

Deux suites d'acceptation, une par jalon. La suite `triage-defauts-reels` verifie que chaque defaut
reel est classe bloquant dans la bonne categorie. La suite `triage-faux-positifs` verifie que les
remarques de style restent mineures et que l'inconnu rend `unknown`. Les deux suites doivent etre
vertes pour que le projet compte comme livrable verifie.

Vous ne livrez qu'un seul fichier : `Submission.cs`. Le classement doit reposer sur une table, pas
sur une suite de branches fragiles.
