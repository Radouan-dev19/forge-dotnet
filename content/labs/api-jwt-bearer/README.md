# Laboratoire — API protégée par jeton Bearer

Ce laboratoire câble ce que les exercices de la semaine 14 font faire à la main : une API
ASP.NET Core réelle dont l'accès est contrôlé par des jetons signés HMAC-SHA256, avec des
politiques d'autorisation par portée. Les exercices enseignent le mécanisme ; ici, vous
configurez le middleware qui l'exécute à chaque requête, et vous vérifiez que la configuration
dit bien ce que vous croyez.

## Ce que contient le dossier

- `src/ForgeJwtLab/` — l'API : `Program.cs` porte `AddAuthentication().AddJwtBearer(...)` et les
  politiques `orders.read` / `orders.write` ; `OrdersController` protège la lecture et l'écriture
  par deux droits distincts ; la sonde `/health` reste anonyme.
- `src/ForgeJwtLab/appsettings.json` — émetteur, audience et clé de signature **factices**. En
  production, la clé viendrait d'un coffre : jamais du dépôt.
- `tests/ForgeJwtLab.Tests/` — la suite qui monte l'API en mémoire et prouve les quatre
  situations : 401 sans jeton, 401 sur jeton expiré, 403 sur portée insuffisante, 200 sur jeton
  valide — plus la clé fausse, l'audience étrangère et l'écriture autorisée.
- `tests/ForgeJwtLab.Tests/TestTokenFactory.cs` — les jetons de test sont fabriqués à la main,
  trois segments et un HMAC, comme dans les exercices : aucun émetteur externe n'est requis.

## Lancer

```powershell
dotnet test content/labs/api-jwt-bearer/tests/ForgeJwtLab.Tests/ForgeJwtLab.Tests.csproj
```

## Ce qu'il faut regarder en premier

Dans `Program.cs`, chaque propriété de `TokenValidationParameters` correspond à une étape de la
chaîne de validation étudiée en leçon : `ValidAlgorithms` impose l'algorithme côté serveur,
`ValidIssuer` et `ValidAudience` bloquent le rejeu croisé, `RequireExpirationTime` refuse les
jetons immortels, `ClockSkew` porte la tolérance d'horloge — trente secondes, une valeur qu'on
doit pouvoir défendre en revue.

La distinction des statuts est le second point : 401 répond « identité non prouvée » — pas de
jeton, signature fausse, jeton périmé — et 403 répond « identité prouvée, droit absent ». La
suite échoue si l'un se substitue à l'autre.

## Ce que ce laboratoire ne montre pas

Il n'y a ni fournisseur d'identité, ni parcours d'obtention du jeton, ni rafraîchissement :
les jetons naissent dans la fabrique de test. C'est un choix — le sujet est la *validation*
côté ressource, pas l'émission — et la limite est déclarée dans le manifeste.
