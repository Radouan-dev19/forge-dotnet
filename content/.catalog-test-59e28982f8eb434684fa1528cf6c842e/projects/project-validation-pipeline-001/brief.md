# Pipeline de validation et contrat d'erreurs

Trois méthodes qui font tourner le **vrai pipeline de validation de .NET** — celui de
`System.ComponentModel.DataAnnotations`, exactement celui qu'ASP.NET Core exécute derrière la
validation de modèle. Vous ne simulez pas des règles : vous exécutez `Validator.TryValidateObject`
sur des modèles annotés, vous l'étendez par un attribut à vous, puis vous projetez ses résultats
dans un contrat d'erreurs qu'un client pourrait consommer.

## Ce qui vous est fourni

Le squelette contient les deux modèles annotés — `OrderDraft` et `ProductDraft` — la coquille de
l'attribut `SkuAttribute`, et `RunPipeline`, l'invocation de référence du validateur avec
`validateAllProperties`. **Ne modifiez ni les modèles ni `RunPipeline`** : les bornes de leurs
attributs sont ce qui rend les cas reproductibles.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string Violations(string name, int quantity, decimal unitPrice);
public static string SkuVerdict(string sku);
public static string ProblemReport(string name, int quantity, decimal unitPrice);
```

### `Violations`

Construit un `OrderDraft` avec les trois valeurs, le passe au pipeline, et rend `valide` si aucun
manquement n'est collecté. Sinon, chaque manquement devient `Membre:code` — le code est le
`ErrorMessage` déclaré par l'attribut — et les entrées sont triées par ordre ordinal puis jointes
par `|`. Deux comportements du pipeline réel sont à connaître : quand l'obligation de présence
échoue, les autres attributs du membre **ne sont pas évalués** — une chaîne vide ne rend donc que
`Name:obligatoire` ; mais un nom présent peut cumuler plusieurs manquements, longueur et alphabet
par exemple, et tous doivent se voir.

### `SkuVerdict`

Le jalon porte d'abord sur `SkuAttribute`, dont `IsValid` est à écrire. Le format accepté est
strict : deux lettres ASCII majuscules, un tiret, quatre chiffres — `AB-1234`. Une valeur absente
est **acceptée** : dans ce pipeline, refuser l'absence est le travail d'un attribut de présence, et
un attribut de format qui s'en mêle devient impossible à composer. `SkuVerdict` construit ensuite un
`ProductDraft`, le valide par le pipeline et restitue le verdict au même format que `Violations`.

### `ProblemReport`

Projette les manquements d'un `OrderDraft` dans un contrat de réponse : `200` si l'objet est
conforme, sinon `422 ` suivi des membres triés, chacun sous la forme `Membre=code1,code2` avec ses
codes triés, les membres étant joints par `;`. L'ordre interne de collecte du validateur n'est pas
contractuel : c'est votre tri qui rend la réponse stable.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les trois doivent être
vertes pour que le projet compte comme livrable vérifié — il satisfait alors l'exigence
**validation et gestion d'erreurs** de la porte B.

## Ce qui n'est pas mesuré

Le câblage HTTP autour de ce pipeline — filtre, ProblemDetails sérialisé, code de statut réellement
émis — appartient au laboratoire d'API. La grille l'observe ; préparez-vous à expliquer où votre
contrat `422` se brancherait.
