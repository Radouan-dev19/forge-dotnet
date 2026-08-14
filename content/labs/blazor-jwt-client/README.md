# Laboratoire — Client Blazor authentifié par jeton

Ce laboratoire câble ce que les exercices de front de la semaine 27 font travailler à la main :
un composant Blazor réel, `OrdersView`, qui ne consomme un client de commandes protégé que
lorsqu'une session authentifiée par jeton Bearer est prouvée. Les exercices enseignent la garde
de route et le contrat client-serveur ; ici, vous écrivez le composant qui les applique à chaque
rendu, et une suite bUnit vérifie qu'il se comporte bien comme vous le croyez.

## Ce que contient le dossier

- `src/ForgeBlazorJwtClient/` — la bibliothèque de composants Razor.
  - `IOrdersClient.cs` — le contrat de lecture des commandes derrière la session authentifiée.
  - `OrdersView.razor` — le composant : il reçoit l'état d'authentification par
    `[CascadingParameter] Task<AuthenticationState>`, ne démarre le chargement que si l'utilisateur
    est authentifié, affiche « Chargement... » pendant l'attente, rend la liste au retour, refuse
    par « Connexion requise » sans identité, et expose un bouton « Annuler » qui annule la
    `CancellationTokenSource` détenue en champ. Le composant est `IDisposable` : il annule et libère
    la source à la destruction.
- `tests/ForgeBlazorJwtClient.Tests/` — la suite bUnit qui rend le composant en mémoire.

## Les trois comportements que la suite prouve

1. **Rendu authentifié** — session autorisée, faux client renvoyant `["A-1","A-2"]` : le balisage
   rendu contient les deux commandes.
2. **Refus non authentifié** — session non autorisée : le balisage affiche « Connexion requise »
   et le faux client n'est jamais appelé.
3. **Annulation en vol** — un chargement resté en attente est annulé au clic sur « Annuler » : le
   jeton passe à `IsCancellationRequested` et le composant affiche « Chargement annulé. ».

## Lancer

```powershell
dotnet test content/labs/blazor-jwt-client/tests/ForgeBlazorJwtClient.Tests/ForgeBlazorJwtClient.Tests.csproj
```

## Note — une preuve automatisée, pas une porte de maîtrise

Contrairement aux laboratoires frères côté Angular et React, où la vérification reste déclarée par
l'apprenant hors bac à sable, la suite bUnit de ce laboratoire s'exécute **à l'intérieur du propre
`dotnet test` de la solution Forge.NET**. C'est donc une preuve automatisée réelle, au niveau de la
suite de tests (côté serveur), et non une simple déclaration : le rendu du composant Razor, la garde
de route et l'annulation sont éprouvés par la machine.

Cela ne change rien au statut de progression : **un laboratoire n'ouvre aucune porte de maîtrise par
conception**. Il ne produit pas d'`achievement` et le manifeste conserve donc la politique
`learner-declared-outside-sandbox` imposée par le schéma. La preuve serveur vit dans l'exécution de
la suite, pas dans une porte franchie.

## La réalité hors ligne

La première exécution de `dotnet test` restaure des paquets NuGet — `bunit` et sa dépendance
`AngleSharp` — depuis le flux de paquets, exactement comme xUnit : une restauration réseau unique.
Une fois le cache peuplé, la suite tourne hors ligne. Aucun navigateur ni conteneur n'est requis :
bUnit rend le composant en mémoire. Un avertissement d'avis NU1902 sur `AngleSharp` peut apparaître
à la restauration ; il est attendu et non bloquant.
