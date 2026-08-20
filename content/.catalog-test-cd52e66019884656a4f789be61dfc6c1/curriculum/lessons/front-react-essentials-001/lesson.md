# React : hooks, dépendances d'effet et état dérivé

## Objectif observable

À la fin de cette leçon, vous saurez respecter les règles d'appel des hooks, régler le tableau de
dépendances d'un `useEffect` sans en oublier ni en trop, distinguer une valeur dérivée d'un état
stocké, choisir entre le contexte et un magasin externe, et reconnaître puis corriger l'effet qui
reboucle parce qu'une dépendance est réallouée à chaque rendu.

## Prérequis

- Avoir lu `front-state-unidirectional-001` : l'état comme source unique et le rendu comme fonction
  de cet état.
- Avoir lu `front-client-server-contract-001` : le contrat d'appel réseau et sa réponse.

## Intuition

React ne suit pas vos hooks par leur nom : il les suit par leur *ordre d'appel*. Chaque rendu
rappelle les hooks dans la même séquence, et React aligne l'état sur cette séquence, position par
position. Tout ce qui casse l'ordre — un hook dans une condition, dans une boucle — désaligne l'état
et corrompt le composant. De cette contrainte découle le reste : un effet ne se rejoue pas quand
vous le décidez, mais quand une de ses dépendances a changé depuis le dernier rendu. Comprendre
React, c'est raisonner sur ce que React compare entre deux rendus, pas sur ce que vous croyez avoir
demandé.

## Explication

**Les règles des hooks sont une conséquence de leur implémentation.** Comme React associe chaque
hook à sa position dans la séquence d'appels, les hooks doivent être appelés au premier niveau du
composant, toujours, dans le même ordre — jamais dans un `if`, une boucle ou après un retour
anticipé. Un hook conditionnel décale toute la suite : le deuxième rendu lit l'état du mauvais
emplacement. Cette règle n'est pas une convention de style, c'est la condition pour que l'état
reste cohérent d'un rendu à l'autre.

**Le tableau de dépendances est une promesse de comparaison.** `useEffect(fn, deps)` rejoue `fn`
quand une valeur de `deps` diffère de sa valeur au rendu précédent — et la comparaison se fait par
identité, pas par contenu. Deux objets de contenu identique mais alloués séparément sont *différents*
pour React. C'est là le noeud. Si vous omettez une dépendance, l'effet lit une valeur périmée
capturée à un rendu antérieur. Si vous mettez une dépendance qui est réallouée à chaque rendu — un
objet littéral, un tableau, une fonction inline — l'effet la voit changer à chaque fois et se
rejoue en boucle. Le tableau de dépendances est donc à ajuster avec la question précise : *cette
valeur a-t-elle la même identité entre deux rendus quand rien n'a réellement changé ?*

**L'état dérivé ne se stocke pas.** Une valeur que l'on peut calculer à partir de l'état ou des
props existants ne doit pas être copiée dans un second `useState` : ce doublon se désynchronise dès
que la source change. On la calcule pendant le rendu, directement, et on ne mémorise par `useMemo`
que si le calcul est réellement coûteux — l'optimisation est secondaire, la correction vient
d'abord. La règle : stocker seulement ce qui ne peut pas être recalculé, dériver tout le reste.
C'est l'application directe de la source unique posée par le socle unidirectionnel.

**Contexte et magasin externe ne jouent pas le même rôle.** Le contexte transmet une valeur à travers
l'arbre sans la passer de main en main ; il rend l'état disponible, mais tout composant qui le
consomme se re-rend quand la valeur du contexte change, même pour une part qui ne l'intéresse pas.
Un magasin externe, lui, laisse chaque composant s'abonner à la seule tranche d'état qu'il lit, donc
ne re-rend que les concernés. Le contexte convient à une donnée stable et globale — thème, session ;
un magasin externe convient à un état qui change souvent et qu'on veut trancher finement.

**Le bug le plus fréquent est l'effet qui reboucle.** Il naît d'une dépendance réallouée à chaque
rendu. L'effet met à jour l'état, ce qui déclenche un rendu, qui réalloue la dépendance, ce qui
rejoue l'effet : la boucle est fermée. Le corriger, ce n'est pas retirer la dépendance — ce serait
capturer une valeur périmée — mais stabiliser son identité, avec `useMemo` pour un objet, `useCallback`
pour une fonction, ou en sortant la constante du composant.

## Exemple commenté

Le noyau transposable est le réducteur pur, l'analogue de `useReducer` : un état plus une action
donnent un nouvel état, sans effet de bord. C'est ce que le runner de Forge fait pratiquer en C#.

```csharp
// Réducteur pur : (état courant, action) -> nouvel état. Aucun effet de bord, aucune mutation.
public static CounterState Reduce(CounterState state, CounterAction action) => action.Kind switch
{
    CounterActionKind.Increment => state with { Count = state.Count + action.Amount },
    CounterActionKind.Decrement => state with { Count = state.Count - action.Amount },
    CounterActionKind.Reset     => state with { Count = 0 },
    _ => state,
};
```

Côté React, `useReducer` consomme exactement cette forme : une fonction pure, appelée par `dispatch`.

```tsx
// Le composant délègue toute transition d'état au réducteur pur ; il ne mute jamais l'état.
const [state, dispatch] = useReducer(reduce, { count: 0 });
return (
  <button onClick={() => dispatch({ kind: "increment", amount: 1 })}>
    {state.count}
  </button>
);
```

## Contre-exemple et erreur fréquente

L'effet ci-dessous reboucle indéfiniment : `options` est un objet littéral réalloué à chaque rendu,
donc jamais égal par identité à celui du rendu précédent.

```tsx
// FAUTIF : options change d'identité à chaque rendu -> l'effet se rejoue en boucle.
function UserPanel({ userId }: { userId: string }) {
  const [data, setData] = useState(null);
  const options = { userId, includeArchived: false }; // réalloué à chaque rendu
  useEffect(() => {
    fetchUser(options).then(setData); // setData -> rendu -> nouvel options -> effet -> ...
  }, [options]);
  return <pre>{JSON.stringify(data)}</pre>;
}
```

La correction stabilise l'identité de la dépendance avec `useMemo` : `options` ne change alors que
si `userId` change réellement.

```tsx
// CORRIGÉ : options garde la même identité tant que userId ne change pas.
const options = useMemo(() => ({ userId, includeArchived: false }), [userId]);
useEffect(() => {
  fetchUser(options).then(setData);
}, [options]); // ne se rejoue que sur un vrai changement de userId
```

## Vérification de compréhension

Avant le quiz, dites à voix haute pourquoi retirer `options` du tableau de dépendances ferait taire
l'avertissement mais introduirait un bug différent.

:::quiz
id=front-react-essentials-001-check
question=Pourquoi un useEffect dépendant d'un objet littéral déclaré dans le corps du composant se rejoue-t-il à chaque rendu ?
option=Parce que React compare les dépendances par contenu et l'objet contient des données différentes
option=Parce que l'objet littéral est réalloué à chaque rendu et React compare les dépendances par identité, donc il paraît toujours changé
option=Parce que useEffect ignore le tableau de dépendances quand une dépendance est un objet
correct=1
success=Exact : la comparaison se fait par identité. Un littéral recréé à chaque rendu n'est jamais identique au précédent, donc l'effet se rejoue. Il faut stabiliser l'identité avec useMemo.
retry=Repensez à ce que React compare entre deux rendus : l'identité des références, pas le contenu des objets.
:::

## Exercice guidé

Ouvrez l'exercice `front-state-reducer-001` dans `/practice`, puis procédez ainsi.

1. Définissez l'état et l'ensemble fermé des actions comme des types explicites.
2. Écrivez le réducteur comme une fonction pure qui, pour chaque action, retourne un nouvel état
   sans muter l'entrée.
3. Traitez l'action inconnue en retournant l'état inchangé.
4. Prédisez l'état résultant d'une séquence d'actions appliquées depuis l'état initial.

## Exercice autonome

Pour une liste filtrable imaginaire, décidez ce qui est état stocké et ce qui est état dérivé : la
requête de recherche, le résultat filtré, le nombre de résultats. Écrivez quel `useState` existe,
ce qui se calcule pendant le rendu, et justifiez pourquoi copier le résultat filtré dans un état
serait une source de désynchronisation.

## Débogage

Un ticket indique : « Le panneau utilisateur envoie des dizaines de requêtes par seconde et l'onglet
chauffe ; ça n'arrive que sur les composants qui chargent des données. »

1. **Symptôme** : rafale de requêtes réseau, un effet qui se rejoue sans arrêt.
2. **Hypothèse** : une dépendance de l'effet est réallouée à chaque rendu — objet, tableau ou
   fonction inline — donc l'effet la voit toujours changer.
3. **Preuve** : lire le tableau de dépendances ; repérer la valeur construite dans le corps du
   composant ; vérifier que l'effet met à jour l'état, refermant la boucle rendu puis réallocation
   puis effet.
4. **Prévention** : stabiliser les dépendances objet/fonction avec `useMemo`/`useCallback`, sortir
   les constantes du composant, et ne jamais faire taire l'avertissement en retirant une dépendance
   réellement lue.

## Entretien

Question posée à voix haute : *pourquoi un `useEffect` peut-il boucler à l'infini, et comment le
corrigez-vous sans introduire de valeur périmée ?*

Une réponse solide explique la comparaison par identité, nomme la dépendance réallouée comme cause,
et distingue les deux mauvaises corrections — retirer la dépendance (valeur périmée) contre
supprimer l'effet — de la bonne : stabiliser l'identité. Elle relie cela à l'état dérivé, qu'on
calcule au rendu plutôt que de le stocker.

## Résumé

- React suit les hooks par ordre d'appel : jamais de hook conditionnel ni dans une boucle.
- Le tableau de dépendances compare par identité, pas par contenu.
- Un effet reboucle quand une dépendance est réallouée à chaque rendu : stabiliser, ne pas retirer.
- L'état dérivé se calcule au rendu ; on ne stocke que ce qui n'est pas recalculable.
- Le contexte diffuse une valeur globale stable ; un magasin externe tranche finement les abonnements.

## Cartes de révision

Question : que compare React pour décider de rejouer un `useEffect` ? Réponse attendue : l'identité
de chaque dépendance par rapport au rendu précédent, pas le contenu des objets ; une référence
réallouée paraît toujours changée.

Question : pourquoi ne faut-il pas stocker une valeur dérivée dans un `useState` ? Réponse attendue :
parce que la copie se désynchronise dès que la source change ; il faut la calculer pendant le rendu
et ne mémoriser que si le calcul est coûteux.

## Test de maîtrise

Sans relire, expliquez pourquoi les hooks doivent être appelés dans un ordre stable, puis décrivez
le cycle exact par lequel un effet reboucle et la correction qui stabilise l'identité. Terminez en
classant trois valeurs d'un écran réel entre état stocké et état dérivé.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
