# Laboratoire — Client Angular de commandes sur API a jeton Bearer

Ce laboratoire fournit un petit client Angular reel qui consomme la ressource `/orders`
de l'API enseignee par le laboratoire `api-jwt-bearer`. L'API valide des jetons Bearer
signes et protege la lecture par la portee `orders.read` ; ici, on travaille l'autre cote
du contrat : attacher le jeton par un intercepteur, gerer l'etat de la vue, se desabonner
proprement, et garder la route selon la presence du jeton.

## Ce que fait le client

- `src/app/token.store.ts` — le magasin du jeton courant. Il **recoit** un jeton par un
  getter (il n'en fabrique aucun) et l'expose a l'intercepteur et a la garde.
- `src/app/auth.interceptor.ts` — un `HttpInterceptor` qui attache l'en-tete
  `Authorization: Bearer <jeton>` a chaque requete sortante de `HttpClient`. Centraliser
  ce geste evite de repeter l'en-tete dans chaque service.
- `src/app/orders.service.ts` — le service `OrdersService` qui appelle `GET /orders` via
  `HttpClient` et type la reponse comme un tableau d'identifiants de commande.
- `src/app/orders.component.ts` — le composant `OrdersComponent`. Sans jeton, il affiche
  **Connexion requise** et n'appelle pas le service. Avec jeton, il charge la liste et
  **ferme son abonnement au demontage** via `takeUntil` sur un `Subject` `destroyed`.
- `src/app/orders.guard.ts` — la garde `canActivate` `ordersGuard` : sans jeton, elle
  redirige vers `/login` (comme un 401 cote navigation) ; avec jeton, elle autorise.
- `src/app/orders.component.spec.ts` — une unique specification Jasmine qui eprouve trois
  cas : connexion requise sans jeton, liste authentifiee, et desabonnement au demontage.

## Comment il se relie a l'API api-jwt-bearer

L'API du laboratoire `api-jwt-bearer` expose `GET /orders` (portee `orders.read`) et
`POST /orders` (portee `orders.write`), et distingue strictement **401** (identite non
prouvee) de **403** (identite prouvee, droit absent). Ce client respecte le meme contrat :
l'intercepteur pose `Authorization: Bearer` sur la requete, et la garde de route reproduit
cote navigation la redirection vers la connexion quand aucun jeton n'est disponible.

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

`npm ci` restaure les dependances depuis le verrou ; `npm test` lance la suite Jasmine via
Karma une seule fois en `ChromeHeadless` (`ng test --watch=false --browsers=ChromeHeadless`).
Un navigateur Chrome doit etre disponible localement.

## Preuve declaree par l'apprenant

Forge.NET n'execute ni navigateur ni npm : **ce laboratoire n'ouvre aucune porte et ne
produit aucune preuve automatisee**. Le succes est **declare par l'apprenant** apres avoir
vu la suite verte en local. C'est un choix assume, inscrit dans le manifeste
(`evidencePolicy: learner-declared-outside-sandbox`).

## Ce que ce laboratoire ne montre pas

Il n'y a ni fournisseur d'identite, ni parcours d'obtention du jeton, ni rafraichissement :
le magasin recoit un jeton par un getter. L'API elle-meme n'est pas incluse — pour un essai
de bout en bout, montez l'API du laboratoire `api-jwt-bearer` a cote.
