# Angular : injection, RxJS et détection de changement

## Objectif observable

À la fin de cette leçon, vous saurez dire comment Angular fournit ses dépendances par injection
plutôt que par import direct, discipliner la désinscription d'un flux RxJS pour ne pas fuir, choisir
la stratégie de détection de changement `OnPush`, attacher un jeton à chaque requête par un
intercepteur, et bloquer une route par un garde `canActivate`.

## Prérequis

- Avoir lu `front-state-unidirectional-001` : le flux de données descendant et l'état qui remonte
  par événements.
- Avoir lu `front-client-server-contract-001` : le contrat requête/réponse et le jeton porteur.

## Intuition

Là où d'autres frameworks laissent le composant importer ses collaborateurs, Angular les lui
*apporte*. Le composant déclare ce dont il a besoin dans son constructeur ; un conteneur central le
lui remet. Cette inversion change tout le reste : les services sont partagés, testables et
remplaçables sans toucher au composant. De la même façon, Angular ne calcule pas l'affichage à
chaque frappe : une zone observe les événements du navigateur et déclenche une passe de détection.
Comprendre Angular, c'est comprendre ces deux mécanismes propres — l'injection et la zone — que le
socle unidirectionnel ne décrit pas.

## Explication

**Le conteneur d'injection est le coeur.** Un service annoté `@Injectable({ providedIn: 'root' })`
est enregistré une fois pour toute l'application ; Angular en crée une seule instance et l'injecte
dans chaque composant qui la demande. Le composant ne fait jamais `new` : il déclare la dépendance
en paramètre de constructeur, et le conteneur résout l'arbre. Cette portée compte. Un service
fourni à la racine est un singleton partagé ; un service fourni au niveau d'un composant obtient une
instance par instance de composant. C'est le levier qui permet de remplacer une implémentation
réelle par une fausse en test, sans que le composant s'en aperçoive, parce qu'il n'a jamais nommé la
classe concrète autrement que comme un type.

**RxJS modélise le temps, pas une valeur.** Un observable n'est pas une donnée : c'est un flux de
valeurs futures auquel on s'abonne. Tant qu'on est abonné, le flux peut émettre ; si le composant
disparaît sans se désabonner, l'abonnement survit et retient le composant en mémoire. C'est la fuite
la plus fréquente en Angular, et elle est silencieuse : rien ne plante, la mémoire grimpe et des
callbacks tournent sur des composants morts. Trois disciplines la ferment. Le pipe `async` dans le
gabarit s'abonne et se désabonne tout seul quand le composant est détruit — c'est le choix par
défaut. Quand on doit s'abonner en TypeScript, on relie l'abonnement au cycle de vie : un sujet
émis dans `ngOnDestroy`, consommé par `takeUntil`, coupe tous les flux d'un coup à la destruction.
La règle mentale est simple : tout `subscribe` explicite doit avoir un plan de mort documenté au
même endroit.

**La détection de changement est déclenchée, pas continue.** Angular s'appuie sur une zone qui
intercepte les événements asynchrones — clic, minuterie, réponse HTTP — et, après chacun, parcourt
l'arbre des composants pour rafraîchir l'affichage. Par défaut, elle vérifie tout l'arbre à chaque
tour, ce qui est correct mais coûteux. La stratégie `OnPush` change le contrat : le composant n'est
revérifié que si une de ses entrées `@Input` change de référence, si un événement part de lui, ou si
un flux qu'il consomme par `async` émet. `OnPush` n'est pas une optimisation cosmétique : c'est un
engagement à traiter les entrées comme immuables, exactement l'invariant que le socle unidirectionnel
pose. Muter un objet en place sous `OnPush` ne redéclenche rien, car la référence n'a pas bougé —
d'où l'obligation de remplacer plutôt que modifier.

**Les formulaires réactifs déclarent l'état, ils ne le lisent pas du DOM.** Un `FormGroup` construit
en TypeScript est la source de vérité du formulaire ; le gabarit s'y lie et reflète sa validité, ses
erreurs, ses changements — exposés comme des observables. C'est cohérent avec le reste : l'état vit
dans le modèle, pas dans les champs.

**L'intercepteur et le garde protègent les bords.** Un intercepteur HTTP s'insère dans chaque
requête sortante : c'est l'endroit unique où l'on attache l'en-tête d'autorisation, sans que chaque
service y pense. Un garde `canActivate` s'insère avant l'activation d'une route : il autorise ou
redirige selon l'état d'authentification. L'un traite le transport, l'autre la navigation ; ensemble
ils évitent d'éparpiller la sécurité dans les composants.

## Exemple commenté

Le noyau transposable est la décision du garde de route : autoriser, ou rediriger. Voici cette
décision isolée en C#, telle que le runner de Forge la fait pratiquer.

```csharp
// Décision pure d'un garde de route : true autorise l'activation, sinon on redirige.
public static RouteGuardDecision CanActivate(bool isAuthenticated, string requestedPath)
{
    if (isAuthenticated)
    {
        return RouteGuardDecision.Allow();
    }

    // Non authentifié : on refuse ET on mémorise la cible pour revenir après connexion.
    return RouteGuardDecision.RedirectTo("/login", returnUrl: requestedPath);
}
```

Côté Angular, le garde et l'intercepteur expriment la même intention, dans le langage du framework.

```typescript
// Garde canActivate : autorise ou redirige vers /login en conservant la cible.
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isAuthenticated()
    ? true
    : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
```

```typescript
// Intercepteur : attache le jeton porteur à chaque requête sortante, en un seul endroit.
export const bearerInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(AuthService).token();
  const authorized = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;
  return next(authorized);
};
```

## Contre-exemple et erreur fréquente

L'erreur classique est un `subscribe` explicite sans plan de désinscription, souvent aggravé par une
mutation en place sous `OnPush`.

```typescript
// FAUTIF : l'abonnement survit au composant (fuite) et on mute l'entrée en place.
ngOnInit() {
  this.clock.tick$.subscribe(v => {
    this.items.push(v);      // mutation : sous OnPush, la vue ne se rafraîchit pas
  });                        // jamais désabonné : fuite mémoire silencieuse
}
```

La correction relie le flux au cycle de vie et remplace la référence plutôt que de muter.

```typescript
// CORRIGÉ : takeUntil coupe le flux à la destruction ; nouvelle référence pour OnPush.
private readonly destroyed$ = new Subject<void>();

ngOnInit() {
  this.clock.tick$
    .pipe(takeUntil(this.destroyed$))
    .subscribe(v => (this.items = [...this.items, v]));
}

ngOnDestroy() {
  this.destroyed$.next();
  this.destroyed$.complete();
}
```

## Vérification de compréhension

Avant le quiz, dites à voix haute pourquoi une mutation `items.push(...)` ne rafraîchit rien sous
`OnPush`, alors qu'elle le ferait avec la stratégie par défaut.

:::quiz
id=front-angular-essentials-001-check
question=Pourquoi un composant en stratégie OnPush ne se rafraîchit-il pas après un items.push(...) ?
option=Parce que OnPush désactive complètement la détection de changement pour ce composant
option=Parce que OnPush ne revérifie que si une entrée change de référence, or push mute le tableau existant sans en changer la référence
option=Parce que push est une opération asynchrone que la zone n'intercepte pas
correct=1
success=Exact : OnPush s'appuie sur le changement de référence des entrées ; muter en place laisse la référence identique, donc rien ne se redéclenche. Il faut remplacer le tableau.
retry=Repensez à ce que OnPush observe précisément : une nouvelle référence d'entrée, pas le contenu de l'objet.
:::

## Exercice guidé

Ouvrez l'exercice `front-route-guard-001` dans `/practice`, puis procédez ainsi.

1. Isolez la décision du garde comme une fonction pure prenant l'état d'authentification et le chemin
   demandé.
2. Retournez une autorisation quand l'utilisateur est authentifié.
3. Sinon, retournez une redirection vers `/login` en conservant le chemin demandé comme cible de
   retour.
4. Prédisez le verdict pour un utilisateur connecté, puis pour un anonyme visant une page protégée.

## Exercice autonome

Concevez pour une application imaginaire la chaîne complète : un service d'authentification fourni à
la racine, un intercepteur qui attache le jeton, et un garde qui protège une zone privée. Écrivez le
flux d'un clic vers une page protégée, alors que la session vient d'expirer, et dites où chaque
maillon intervient.

## Débogage

Un ticket indique : « Après quelques minutes de navigation, l'onglet consomme de plus en plus de
mémoire et le profil montre des composants détruits toujours vivants. »

1. **Symptôme** : croissance mémoire continue, instances de composants retenues après destruction.
2. **Hypothèse** : un `subscribe` explicite sans désinscription retient chaque composant via son
   abonnement toujours actif.
3. **Preuve** : chercher les `subscribe` sans `takeUntil` ni pipe `async` ; vérifier qu'un
   `ngOnDestroy` coupe bien le flux ; observer si le compte d'instances retenues cesse de croître
   après correction.
4. **Prévention** : préférer le pipe `async` par défaut, imposer un `takeUntil(this.destroyed$)`
   sur tout abonnement manuel, et le vérifier en revue.

## Entretien

Question posée à voix haute : *comment évitez-vous les fuites d'abonnement en Angular, et que change
`OnPush` pour vous ?*

Une réponse solide nomme les trois disciplines de désinscription — pipe `async`, `takeUntil` relié à
`ngOnDestroy`, désabonnement explicite — et explique que `OnPush` engage à traiter les entrées comme
immuables, donc à remplacer les références plutôt qu'à muter. Elle relie ce dernier point à
l'injection : services partagés, entrées descendantes, détection déclenchée par la zone.

## Résumé

- Angular apporte ses dépendances par injection ; le composant les déclare, le conteneur les résout.
- Tout `subscribe` explicite doit avoir un plan de désinscription : pipe `async` ou `takeUntil`.
- `OnPush` ne revérifie que sur changement de référence d'entrée : remplacer, jamais muter.
- L'intercepteur attache le jeton en un point unique ; le garde `canActivate` décide la navigation.
- Les formulaires réactifs gardent l'état dans le modèle, pas dans le DOM.

## Cartes de révision

Question : quelles sont les trois façons de fermer une fuite d'abonnement RxJS ? Réponse attendue :
le pipe `async` dans le gabarit, `takeUntil` relié à un sujet émis dans `ngOnDestroy`, ou un
désabonnement explicite du `Subscription` conservé.

Question : que doit garantir un composant `OnPush` sur ses entrées ? Réponse attendue : qu'elles
changent de référence à chaque changement de valeur, car `OnPush` ne revérifie que sur nouvelle
référence, jamais sur mutation en place.

## Test de maîtrise

Sans relire, décrivez comment Angular résout une dépendance de la racine jusqu'au composant, puis le
parcours d'une requête sortante à travers l'intercepteur et d'une navigation à travers le garde.
Expliquez enfin pourquoi `OnPush` et la discipline de désinscription reposent tous deux sur
l'immuabilité des entrées.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
