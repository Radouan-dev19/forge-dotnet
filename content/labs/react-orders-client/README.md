# Laboratoire — Client React de commandes sur API a jeton Bearer

Ce laboratoire fournit un petit client React reel qui consomme la ressource `/orders`
de l'API enseignee par le laboratoire `api-jwt-bearer`. L'API valide des jetons Bearer
signes et protege la lecture par la portee `orders.read` ; ici, on travaille l'autre
cote du contrat : attacher le jeton, gerer les etats d'interface, annuler proprement, et
router selon les droits portes par le jeton.

## Ce que fait le client

- `src/OrdersService.ts` — le service d'acces. Il recoit un **getter de jeton** (il ne
  fabrique aucun jeton) et emet un `GET /orders` en attachant l'en-tete
  `Authorization: Bearer <jeton>`. Il traduit les statuts de l'API en erreurs distinctes :
  `login-required` sans jeton, `unauthorized` sur 401, `forbidden` sur 403. La requete
  accepte un `AbortSignal` pour etre annulable.
- `src/OrdersView.tsx` — le hook `useOrders` et le composant `OrdersView`. Sans jeton, la
  vue affiche **Connexion requise** et n'emet aucun appel. Avec jeton, elle charge la liste
  et, si le composant est demonte pendant le chargement, elle **annule la requete en vol**
  via un `AbortController`.
- `src/routeGuard.ts` — l'aide de garde de route. Elle decode la charge utile du jeton
  (sans en verifier la signature, qui reste l'affaire du serveur) et tranche entre
  `allow`, `redirect` (connexion requise, comme un 401) et `forbidden` (droit manquant,
  comme un 403).
- `src/OrdersView.test.tsx` — une unique specification Vitest qui eprouve trois cas :
  rendu authentifie, refus sans jeton, et annulation au demontage.

## Comment il se relie a l'API api-jwt-bearer

L'API du laboratoire `api-jwt-bearer` expose `GET /orders` (portee `orders.read`) et
`POST /orders` (portee `orders.write`), et distingue strictement **401** (identite non
prouvee) de **403** (identite prouvee, droit absent). Ce client respecte le meme contrat :
le jeton part en en-tete `Authorization: Bearer`, et l'aide de garde reproduit cote client
la distinction 401 / 403 pour router l'interface sans attendre l'aller-retour reseau.

## Installation reseau requise

**Lisez ceci avant de lancer.** Ce laboratoire est le **seul** du parcours a exiger une
installation reseau. Tout le reste du parcours fonctionne hors ligne.

- **Installation unique** : la premiere execution de `npm ci` telecharge les paquets. Une
  connexion sortante est necessaire cette fois-la ; ensuite, le cache local suffit.
- **Seul palier reseau du parcours** : partout ailleurs, aucune connexion n'est requise.
- **Versions figees et verrou committe** : `package.json` epingle chaque version a l'exact
  (ni `^` ni `~`), et le `package-lock.json` committe gele l'arbre complet avec ses
  empreintes d'integrite. `npm ci` restaure exactement ce verrou.
- **node_modules non versionne** : le dossier `node_modules/` est ignore (`.gitignore`) et
  n'est jamais committe. `npm ci` le reconstruit localement.

## Lancer

```powershell
npm ci
npm test
```

`npm ci` restaure les dependances depuis le verrou ; `npm test` lance Vitest une seule fois
(`vitest run`) en environnement jsdom.

## Preuve declaree par l'apprenant

Forge.NET n'execute ni navigateur ni npm : **ce laboratoire n'ouvre aucune porte et ne
produit aucune preuve automatisee**. Le succes est **declare par l'apprenant** apres avoir
vu la suite verte en local. C'est un choix assume, inscrit dans le manifeste
(`evidencePolicy: learner-declared-outside-sandbox`).

## Ce que ce laboratoire ne montre pas

Il n'y a ni fournisseur d'identite, ni parcours d'obtention du jeton, ni rafraichissement :
le service recoit un getter de jeton. L'API elle-meme n'est pas incluse — pour un essai de
bout en bout, montez l'API du laboratoire `api-jwt-bearer` a cote.
