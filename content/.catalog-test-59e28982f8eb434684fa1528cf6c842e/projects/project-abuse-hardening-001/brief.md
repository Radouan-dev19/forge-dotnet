# Durcissement contre l'abus

Trois défenses écrites avec la **cryptographie et les règles réelles de la BCL**, éprouvées contre
des tentatives d'abus que l'énoncé ne liste pas. C'est la règle du jeu de ce projet, et elle est
assumée : une défense dont les attaques sont énumérées d'avance ne mesure que la lecture de
l'énoncé. Les cas cachés contiennent des variantes hostiles ; votre code doit tenir parce qu'il
applique la règle, pas parce qu'il connaît la liste.

## Ce qui vous est fourni

Le squelette ne contient que les trois signatures : les règles ci-dessous sont le contrat complet,
et leur mise en œuvre vous appartient. `System.Security.Cryptography` est disponible dans le bac à
sable, `CryptographicOperations.FixedTimeEquals` compris.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string SignatureVerdict(string payload, string secret, string signature);
public static string SafePath(string requested);
public static string AdmitVerdict(string journal, string request, int windowSeconds);
```

### `SignatureVerdict`

Recalcule le HMAC-SHA256 du contenu (UTF-8) avec la clé (UTF-8), encode le condensat en
**Base64Url sans remplissage** — `+` devient `-`, `/` devient `_`, aucun `=` — et compare avec la
signature présentée par une égalité **en temps constant**. Rend `authentique` ou `refus`. La forme
Base64 standard, un remplissage résiduel ou une signature vide sont des refus : deux encodages
valides du même condensat, c'est deux jetons acceptés pour un seul émis.

### `SafePath`

Canonise un chemin demandé avant toute décision, dans cet ordre :

1. décoder les séquences pour-cent **une seule fois** — un `%` non suivi de deux chiffres
   hexadécimaux vaut `refus:caractere` ;
2. remplacer chaque barre oblique inversée par une barre oblique ;
3. un chemin commençant par `/` vaut `refus:absolu` ;
4. tout caractère hors de `a-z A-Z 0-9 . _ - /` vaut `refus:caractere` ;
5. découper sur `/`, ignorer les segments vides et `.` ; le moindre segment `..` vaut
   `refus:remontee` — on ne résout pas une remontée, on la refuse ;
6. s'il ne reste aucun segment, `refus:vide` ; sinon rendre les segments joints par `/`.

L'encodage doublé est le piège classique : décodé une fois, `%252e` redevient `%2e`, et ce `%`
résiduel doit être refusé comme caractère, jamais décodé une seconde fois.

### `AdmitVerdict`

Le journal liste les requêtes admises, sous la forme `nonce@secondes` jointes par `;` — il peut
être vide. L'instant courant est l'horodatage le plus récent du journal, ou celui de la requête si
le journal est vide. Dans cet ordre :

- une requête dont l'écart à l'instant courant dépasse la fenêtre vaut `refus:horodatage`,
  la borne exacte étant admise ;
- un nonce déjà présent dans le journal à un horodatage encore dans la fenêtre vaut
  `refus:rejeu` ; passé la fenêtre, le nonce redevient libre ;
- sinon `admis`.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les trois doivent être
vertes pour que le projet compte comme livrable vérifié — il satisfait alors l'exigence
**sécurité appliquée** de la porte D.

## Ce qui n'est pas mesuré

La résistance temporelle réelle de votre comparaison : une suite fonctionnelle vérifie que vous
appelez une égalité en temps constant sur le bon contenu, elle ne chronomètre pas. La grille
l'observe ; sachez expliquer ce qu'une comparaison ordinaire laisserait fuir.
