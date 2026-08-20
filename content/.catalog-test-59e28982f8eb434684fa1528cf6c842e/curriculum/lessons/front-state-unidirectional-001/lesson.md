# État et flux de données unidirectionnel

## Objectif observable

À la fin de cette leçon, vous saurez tracer le sens de circulation dans une interface — les
données descendent, les événements remontent —, désigner la source unique de vérité, distinguer ce
qui doit être stocké de ce qui doit être recalculé, et expliquer pourquoi modifier en place un état
partagé rend l'application imprévisible.

## Prérequis

- Avoir lu `front-rendering-reconciliation-001` : la vue comme fonction de l'état.
- Savoir qu'une même référence partagée par deux endroits les rend solidaires de toute mutation.

## Intuition

Pensez à une chaîne de commandement claire : les ordres descendent, les comptes rendus remontent,
et personne ne modifie l'ordre du voisin dans son dos. Une interface bien construite fonctionne de
même. La donnée part d'un point haut et descend, inchangée, vers les composants qui l'affichent ;
quand l'utilisateur agit, le composant ne corrige pas la donnée lui-même, il signale l'événement
vers le haut, là où la donnée est détenue. Ce sens unique rend chaque changement traçable jusqu'à
sa cause. Dès qu'un composant modifie une donnée qu'il n'est censé qu'afficher, la chaîne se
brouille et l'on ne sait plus qui a changé quoi.

## Explication

**Les données descendent, les événements remontent.** Un composant reçoit des données de son
parent et les affiche ; il ne les possède pas, il les emprunte en lecture. Lorsque l'utilisateur
interagit, le composant émet un événement — une notification d'intention — vers le parent, qui
détient la donnée et décide s'il la change. Ce couple descente des données, remontée des événements
est le flux unidirectionnel. Sa vertu est la traçabilité : toute valeur affichée a un seul chemin
qui explique sa venue, et tout changement un seul endroit qui a pu le décider.

**La source unique de vérité interdit les copies concurrentes.** Chaque donnée doit être détenue
à un seul endroit, celui qui en est responsable ; tous les autres la lisent depuis là. Le piège est
de recopier une valeur dans un composant enfant « pour aller plus vite » : on obtient deux versions
qui divergent dès que l'une change sans l'autre, et l'affichage devient incohérent sans qu'on sache
laquelle croire. Une seule vérité, lue partout, garantit qu'il n'y a jamais deux réponses à la même
question.

**L'état dérivé se calcule, il ne se stocke pas.** Certaines valeurs sont fondamentales : la liste
des articles d'un panier. D'autres en découlent : le nombre d'articles, le total à payer. La
tentation est de stocker aussi ces valeurs dérivées et de les maintenir à la main. C'est une
source d'incohérence : dès qu'on oublie une mise à jour, le total ment. La règle est de ne stocker
que l'état fondamental et de recalculer le dérivé à chaque rendu, où il ne peut jamais se
désynchroniser. Se demander « puis-je le recalculer à partir d'autre chose ? » avant de stocker
une valeur élimine une grande part des bogues d'état.

**La mutation partagée casse la prévisibilité.** Modifier en place un objet — changer un champ
d'un objet existant plutôt que produire un nouvel objet — pose deux problèmes. D'abord, si cet
objet est partagé par référence avec un autre endroit, ce dernier voit sa donnée changer sans
l'avoir demandé : un effet à distance, invisible à la lecture du code. Ensuite, le framework
compare souvent les états par identité de référence pour décider s'il faut re-rendre ; muter en
place laisse la référence inchangée, si bien qu'un changement réel peut passer inaperçu et
l'écran rester figé. La mutation partagée combine donc deux maux : des changements fantômes chez
les autres et des changements ignorés chez soi.

**L'immutabilité rend chaque transition explicite.** L'alternative est de ne jamais modifier
l'état existant, mais de produire un nouvel état à partir de l'ancien. Chaque changement devient
une valeur neuve, distincte de la précédente, ce qui rend la comparaison par référence fiable et
supprime les effets à distance : personne ne partage un objet mutable. On peut même conserver les
états successifs, ce qui autorise l'annulation ou le rejeu.

**Le reducer centralise la logique de transition.** Un reducer est une fonction pure qui prend
l'état courant et une action décrivant ce qui s'est passé, et rend le nouvel état, sans rien
modifier en place. Toute la logique de « comment l'état change » tient à un seul endroit : « pour
telle action, voici le nouvel état ». C'est le prolongement du flux unidirectionnel : les
événements remontent sous forme d'actions, le reducer décide, un nouvel état redescend. Étant
pure, la fonction se teste avec un état et une action, sans interface ni écran.

## Exemple commenté

Le coeur d'un reducer, en C# : une fonction pure qui applique une action à un état sans jamais le
modifier en place.

```csharp
public record Cart(int Count, decimal Total);
public abstract record Action;
public sealed record AddItem(decimal Price) : Action;
public sealed record Clear() : Action;

// Fonction pure : meme entree, meme sortie ; aucun champ mute, un nouvel etat est rendu.
public static Cart Reduce(Cart state, Action action) => action switch
{
    AddItem a => state with { Count = state.Count + 1, Total = state.Total + a.Price },
    Clear     => new Cart(0, 0m),
    _         => state
};
```

Le point à retenir : `with` produit un nouvel enregistrement au lieu de toucher l'ancien ;
l'ancien état reste valable, et la transition est entièrement décrite par le couple état-action.

## Contre-exemple et erreur fréquente

L'erreur classique mute en place un état partagé et stocke un dérivé maintenu à la main.

```csharp
// FAUTIF : on mute la liste partagee et on maintient un total en double.
sharedItems.Add(price);   // meme reference vue ailleurs : effet a distance
cachedTotal += price;     // dependance stockee qui derivera tot ou tard
```

Symptôme : un autre écran affiche soudain un article qu'il n'a pas ajouté, et le total finit par
ne plus correspondre à la liste après quelques opérations. La cause est double : le partage par
référence propage la mutation, et le total stocké se désynchronise. La correction produit un
nouvel état et recalcule le dérivé.

```csharp
// CORRIGE : nouvel etat immuable ; le total est derive, jamais stocke separement.
var next = new Cart(state.Count + 1, state.Total + price);
// Total lu directement depuis next.Total : une seule source de verite.
```

## Vérification de compréhension

Avant le quiz, dites à voix haute : dans « les données descendent, les événements remontent », que
fait un composant enfant quand l'utilisateur clique, et que ne fait-il surtout pas ?

:::quiz
id=front-state-unidirectional-001-check
question=Pourquoi vaut-il mieux recalculer le total d'un panier à chaque rendu plutôt que le stocker et le maintenir à la main ?
option=Parce que recalculer est toujours plus rapide que lire une valeur déjà stockée
option=Parce qu'une valeur dérivée stockée peut se désynchroniser de sa source dès qu'on oublie une mise à jour, alors qu'une valeur recalculée depuis la source unique reste toujours cohérente
option=Parce que les frameworks interdisent de stocker des nombres calculés
correct=1
success=Exact : l'état dérivé recalculé depuis la source unique ne peut pas mentir ; stocké, il dérive au premier oubli de mise à jour.
retry=Demandez-vous ce qui arrive au total stocké le jour où une opération modifie la liste sans le recalculer.
:::

## Exercice guidé

Ouvrez l'exercice `front-state-reducer-001` dans `/practice`, puis procédez ainsi.

1. Identifiez l'état fondamental à stocker et distinguez-le des valeurs qui en dérivent.
2. Écrivez le reducer comme une fonction pure qui rend un nouvel état pour chaque action.
3. Vérifiez qu'aucune branche ne modifie l'état reçu en place ; chaque cas produit une valeur neuve.
4. Confirmez que le total n'est jamais stocké séparément mais recalculé depuis l'état fondamental.

## Exercice autonome

Choisissez un petit écran interactif — un compteur avec historique, un filtre de liste. Listez son
état fondamental, ses valeurs dérivées, et les actions possibles. Écrivez le reducer correspondant
en veillant à ne muter aucun état, puis rejouez une séquence d'actions à la main pour vérifier que
chaque état successif est cohérent et qu'aucune valeur dérivée n'a été stockée.

## Débogage

Un ticket indique : « Deux onglets de l'application affichent des paniers différents alors qu'ils
devraient montrer le même ; parfois le total ne correspond pas au nombre d'articles. »

1. **Symptôme** : deux vues censées lire la même donnée divergent, et un dérivé contredit sa source.
2. **Hypothèse** : la donnée a été recopiée au lieu d'être lue depuis une source unique, et le
   total est stocké au lieu d'être recalculé.
3. **Preuve** : tracer d'où chaque vue lit son panier ; s'il existe deux copies, les fusionner en
   une source unique et remplacer le total stocké par un calcul.
4. **Prévention** : une seule détentrice par donnée, un flux unidirectionnel strict, et aucun état
   dérivé stocké quand il peut être recalculé.

## Entretien

Question posée à voix haute : *qu'appelle-t-on flux de données unidirectionnel, et en quoi
l'immutabilité de l'état sert-elle ce flux ?*

Une réponse solide décrit la descente des données et la remontée des événements, nomme la source
unique de vérité et la distinction stocké contre dérivé, puis relie l'immutabilité à deux gains :
comparaison par référence fiable et absence d'effets à distance. Elle situe le reducer comme la
fonction pure qui centralise les transitions et se teste sans interface.

## Résumé

- Les données descendent, les événements remontent : chaque valeur et chaque changement a un chemin unique.
- Une donnée n'a qu'une source de vérité ; recopier crée des versions qui divergent.
- On stocke l'état fondamental et on recalcule le dérivé, qui ne peut alors pas se désynchroniser.
- Muter un état partagé propage des effets à distance et masque des changements au framework.
- Un reducer pur centralise les transitions et se teste avec un état et une action.

## Cartes de révision

Question : pourquoi la mutation en place d'un état partagé nuit-elle à la fois aux autres et au
rendu ? Réponse attendue : elle change la donnée d'autres endroits qui partagent la référence
(effet à distance) et laisse la référence inchangée, si bien qu'un changement réel peut échapper à
la comparaison qui décide du re-rendu.

Question : comment distinguer un état à stocker d'un état à dériver ? Réponse attendue : si la
valeur peut être recalculée à partir d'une autre donnée déjà détenue, elle est dérivée et doit
être recalculée ; sinon elle est fondamentale et doit être stockée à une source unique.

## Test de maîtrise

Sans relire, tracez le trajet d'une interaction dans un flux unidirectionnel : événement remonté,
action, reducer, nouvel état redescendu. Puis expliquez, sur un panier, pourquoi le total doit
être dérivé et pourquoi produire un nouvel état plutôt que muter l'ancien préserve la
prévisibilité.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
