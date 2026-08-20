# Rendu et réconciliation : du modèle à l'écran

## Objectif observable

À la fin de cette leçon, vous saurez formuler que l'écran affiché résulte d'un calcul à partir de
l'état courant, décrire la réconciliation par laquelle le framework compare la nouvelle description
d'interface à celle en place pour ne modifier que les éléments réellement différents, et expliquer
pourquoi une identité instable dans une liste force le remplacement de fragments inchangés.

## Prérequis

- Avoir lu `reference-types-001` : la différence entre une valeur et la référence qui la désigne.
- Savoir qu'une comparaison peut porter soit sur le contenu, soit sur l'identité d'un objet.

## Intuition

Décrivez un tableau blanc à un assistant qui ne voit pas l'écran : plutôt que de tout effacer et
tout réécrire, vous lui indiquez seulement ce qui a bougé. Un framework de rendu procède ainsi.
Vous ne manipulez pas l'écran directement ; vous produisez, à partir de l'état, une description
complète de ce qu'il devrait montrer. Le framework conserve la description précédente, la compare
à la nouvelle, et n'applique à l'affichage réel que leur différence. Recalculer une description
entière est bon marché ; toucher l'affichage réel est coûteux. Tout l'art tient dans cet écart.

## Explication

**L'interface est une fonction pure de l'état.** Le modèle mental central est une équation :
vue égale fonction appliquée à l'état. Vous ne décrivez jamais comment passer de l'ancien écran
au nouveau ; seulement à quoi l'écran doit ressembler pour l'état actuel. Chaque changement d'état
rappelle cette fonction, qui rend une description fraîche, entière, indépendante de la précédente.
Cette approche déclarative supprime les bogues de transition manuelle oubliée : reconstruite d'un
bloc, la description ne peut pas rester à moitié à jour.

**Le rendu produit une description, pas des ordres d'affichage.** Le résultat de la fonction
n'est pas encore l'écran : c'est un arbre d'objets légers décrivant les éléments souhaités et leurs
attributs. Le construire ne coûte presque rien car il ne touche rien de visible. Le framework
dispose donc de deux arbres : celui affiché à l'écran, et celui que vous venez de rendre. Son
travail est de faire converger le premier vers le second au moindre coût.

**La réconciliation est la comparaison des deux arbres.** Nœud par nœud, le framework parcourt
l'ancienne et la nouvelle description en parallèle. Si un nœud est du même type au même endroit, il
le conserve et met à jour les attributs qui diffèrent — un texte, une couleur. Si le type diffère,
il ne peut réutiliser l'existant : il détruit le sous-arbre et en crée un neuf. Cette différence
entre mise à jour et remplacement est l'axe de la leçon. Mettre à jour garde l'élément vivant et
change ce qui a bougé ; remplacer jette l'élément et son contenu, y compris son état interne et sa
position de défilement.

**Re-rendre n'est pas re-monter.** Un re-rendu rappelle la fonction et recalcule la description ;
il est fréquent et peu coûteux. Un re-montage détruit l'élément affiché puis en instancie un
nouveau ; il est rare, coûteux, et efface l'état local de l'élément. Confondre les deux mène à des
raisonnements faux : on croit optimiser un re-rendu alors que le vrai problème est un re-montage
provoqué sans le vouloir. Le re-rendu compare ; le re-montage recommence de zéro.

**L'identité gouverne les listes.** Pour une liste d'éléments, le framework ne peut pas deviner
si le troisième élément d'hier est le même que le troisième d'aujourd'hui, ou si l'on a inséré un
nouvel élément en tête qui a décalé tous les autres. Il a besoin d'une clé stable : un
identifiant propre à chaque élément, indépendant de sa position. Avec des clés stables, insérer
un élément au début n'entraîne que la création de ce seul nœud ; les autres sont reconnus et
conservés. Sans clé, ou avec l'indice de position comme clé, le framework aligne les éléments par
rang : insérer en tête lui fait croire que tout a changé, et il remplace des éléments qui étaient
pourtant identiques. La clé est le nom qui permet de suivre un élément à travers les rendus.

**Les rendus gaspillés viennent d'identités instables.** Quand une valeur transmise à un
sous-composant est recréée à chaque rendu — une nouvelle référence pour un contenu pourtant
identique —, le framework compare les références, les trouve différentes, et re-rend sans
nécessité. Le contenu n'a pas bougé, mais l'identité si. C'est la source la plus courante de
rendus inutiles : non pas trop d'état, mais des références neuves pour des données inchangées.
Réutiliser la même référence tant que le contenu ne change pas laisse la réconciliation conclure
« rien à faire ».

## Exemple commenté

Le coeur de la réconciliation, transposé en C# : comparer deux descriptions et décider, pour
chaque nom, s'il faut conserver, mettre à jour, créer ou supprimer.

```csharp
public enum Change { Keep, Update, Mount, Unmount }

// Compare l'ancien et le nouvel etat d'un noeud identifie par une cle stable.
public static Change Reconcile(Node? previous, Node? next)
{
    if (previous is null && next is not null) return Change.Mount;   // apparait
    if (previous is not null && next is null) return Change.Unmount; // disparait
    if (previous!.Type != next!.Type)         return Change.Mount;   // type different => remontage

    // Meme type au meme endroit : on garde l'element, on met a jour si le contenu differe.
    return previous.Content == next.Content ? Change.Keep : Change.Update;
}
```

Le point à retenir : un changement de type force un remontage complet, alors qu'un simple
changement de contenu se règle par une mise à jour ciblée qui préserve l'élément vivant.

## Contre-exemple et erreur fréquente

L'erreur classique est d'identifier les éléments d'une liste par leur position plutôt que par une
clé stable.

```csharp
// FAUTIF : la cle est l'indice de position. Inserer en tete decale tout le monde.
var keyed = items.Select((item, index) => (Key: index.ToString(), item));
```

Symptôme : on insère un élément en haut de la liste et les champs de saisie des autres lignes
affichent soudain les mauvaises valeurs, ou une animation rejoue partout. Le framework a aligné par
rang : l'élément d'indice zéro d'hier est comparé au nouvel indice zéro, jugé « mis à jour », et son
état interne reste accroché au mauvais contenu. La correction utilise une identité propre à la
donnée.

```csharp
// CORRIGE : la cle vient de la donnee elle-meme, stable a travers les insertions.
var keyed = items.Select(item => (Key: item.Id, item));
```

## Vérification de compréhension

Avant le quiz, dites à voix haute : quelle différence de conséquence entre un re-rendu et un
re-montage sur l'état local d'un élément ?

:::quiz
id=front-rendering-reconciliation-001-check
question=Pourquoi une liste dont les éléments sont identifiés par leur indice de position pose-t-elle problème quand on insère un élément en tête ?
option=Parce que le calcul des indices ralentit proportionnellement à la taille de la liste
option=Parce que le framework aligne les éléments par rang : il croit que chaque position a changé de contenu et met à jour ou remonte des éléments qui étaient pourtant les mêmes
option=Parce que les indices numériques ne peuvent pas servir de clés dans la plupart des frameworks
correct=1
success=Exact : sans identité propre à la donnée, l'insertion en tête décale les rangs et le framework confond des éléments distincts, corrompant leur état interne.
retry=Repensez à ce que la clé permet de suivre à travers les rendus, et à ce qui se passe quand tous les rangs se décalent.
:::

## Exercice guidé

Ouvrez l'exercice `front-state-reducer-001` dans `/practice`, puis procédez ainsi.

1. Repérez, dans le scénario, quel changement d'état déclenche un nouveau rendu de la liste.
2. Pour chaque élément, dites si la clé fournie est stable ou dérivée de la position.
3. Prédisez, pour une insertion en tête, quels éléments sont conservés et lesquels sont remontés.
4. Corrigez la clé pour qu'elle vienne de l'identifiant de la donnée, puis reprédisez le résultat.

## Exercice autonome

Choisissez une interface comportant une liste modifiable. Décrivez sa vue comme une fonction de
l'état, puis simulez trois changements : ajout en tête, suppression au milieu, modification d'un
champ. Pour chacun, notez quels nœuds sont conservés, mis à jour, créés ou détruits, selon que les
clés sont stables ou positionnelles.

## Débogage

Un ticket indique : « Quand on ajoute une tâche en haut de la liste, le texte déjà tapé dans les
autres champs de saisie saute d'une ligne à l'autre. »

1. **Symptôme** : l'état local des champs suit le rang, pas la donnée, lors d'une insertion en tête.
2. **Hypothèse** : les éléments sont identifiés par leur indice ; l'insertion décale les rangs et
   la réconciliation associe chaque champ au mauvais élément.
3. **Preuve** : remplacer la clé positionnelle par l'identifiant de la donnée et vérifier que le
   texte reste attaché à sa ligne après une insertion.
4. **Prévention** : toujours fournir une clé stable issue de la donnée pour toute liste dont
   l'ordre peut changer, et ne jamais utiliser l'indice quand des insertions sont possibles.

## Entretien

Question posée à voix haute : *que fait exactement un framework quand l'état change, et pourquoi
l'écran ne clignote-t-il pas alors que la description est entièrement recalculée ?*

Une réponse solide pose l'équation vue égale fonction de l'état, distingue la description
recalculée de l'affichage réel coûteux, et décrit la réconciliation comme la comparaison qui ne
patche que la différence. Elle sépare re-rendu et re-montage, et relie les rendus gaspillés à des
identités instables.

## Résumé

- L'écran résulte d'une fonction appliquée à l'état : on décrit la cible, pas les transitions.
- Le rendu produit une description légère ; la réconciliation la compare à l'existant et ne patche que ce qui diffère.
- Mettre à jour préserve l'élément et son état ; remplacer le détruit — re-rendre n'est pas re-monter.
- Les clés stables suivent un élément à travers les rendus ; l'indice de position corrompt les listes réordonnées.
- Les rendus inutiles viennent d'identités recréées pour un contenu inchangé.

## Cartes de révision

Question : quelle est la différence entre re-rendre et re-monter un élément ? Réponse attendue :
re-rendre recalcule la description et met à jour ce qui diffère en gardant l'élément vivant ;
re-monter détruit l'élément et en crée un neuf, ce qui efface son état local.

Question : pourquoi une référence recréée à chaque rendu provoque-t-elle un rendu inutile ?
Réponse attendue : le framework compare les identités ; une nouvelle référence pour un contenu
identique est jugée différente, ce qui déclenche un re-rendu que le contenu ne justifiait pas.

## Test de maîtrise

Sans relire, décrivez le trajet complet d'un changement d'état jusqu'à l'écran : fonction
rappelée, description produite, réconciliation, patch minimal. Puis expliquez, sur une insertion en
tête de liste, pourquoi la stabilité des clés change le nombre de nœuds remontés.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
