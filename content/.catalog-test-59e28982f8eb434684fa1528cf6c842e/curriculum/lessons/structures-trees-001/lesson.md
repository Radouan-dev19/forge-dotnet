# Arbres minimaux et parcours

## Objectif observable

À la fin de cette leçon, vous saurez distinguer hauteur, profondeur et taille sans les confondre,
choisir le parcours qui correspond à la question posée, et écrire la version itérative d'un parcours
récursif lorsque la profondeur n'est pas bornée.

## Prérequis

- Avoir lu `structures-dictionaries-recursion-001` et savoir estimer la profondeur d'une récursion.
- Savoir déclarer une classe qui se référence elle-même.

## Intuition

Un arbre est la structure des données qui contiennent d'autres données du même genre : un dossier
contient des dossiers, une catégorie contient des sous-catégories, un commentaire porte des réponses.
Chaque nœud a un parent unique et zéro, un ou plusieurs enfants, sans cycle.

Le point qui décide de tout, ensuite, est **l'ordre dans lequel on visite les nœuds** — parce que cet
ordre est la réponse à une question métier différente à chaque fois.

## Explication

**Trois mesures qu'on confond systématiquement.** La *taille* est le nombre total de nœuds. La
*profondeur d'un nœud* est sa distance à la racine — la racine est à la profondeur zéro. La *hauteur
de l'arbre* est la profondeur maximale, autrement dit la longueur du plus long chemin descendant.

L'erreur la plus fréquente est de compter la hauteur en nœuds pour certains cas et en arêtes pour
d'autres. Choisissez une convention, écrivez-la dans le contrat de la méthode, et tenez-vous-y : un
arbre à un seul nœud a une hauteur de 1 en convention « nœuds » et de 0 en convention « arêtes ». Les
deux sont défendables ; le mélange ne l'est pas.

**Quatre parcours, quatre questions.** Le parcours *préfixe* visite le nœud puis ses enfants : c'est
l'ordre naturel pour copier une arborescence ou l'afficher indentée, parce qu'un parent est traité
avant ce qu'il contient. Le parcours *suffixe* visite les enfants puis le nœud : c'est l'ordre de la
suppression et du calcul agrégé, parce qu'on a besoin du résultat des enfants avant de traiter le
parent — calculer la taille d'un dossier en est l'exemple type.

Le parcours *infixe* n'a de sens que sur un arbre binaire, et il porte une propriété précieuse : sur
un arbre binaire de recherche, il énumère les valeurs **dans l'ordre croissant**. Le parcours *en
largeur*, enfin, visite niveau par niveau : c'est la réponse aux questions de plus court chemin en
nombre d'étapes, et à celles qui portent sur un niveau donné.

Le tableau se retient facilement : *avant les enfants* pour copier, *après les enfants* pour agréger
et supprimer, *infixe* pour l'ordre, *largeur* pour la distance.

**Le code des trois premiers est identique à une ligne près.** Ce qui change, c'est **où** l'on place
le traitement du nœud par rapport aux appels sur les enfants. Voir cela une fois évite de mémoriser
trois algorithmes.

**Profondeur et largeur diffèrent seulement par la structure d'attente.** Un parcours en profondeur
itératif utilise une pile ; le même code avec une file donne un parcours en largeur. C'est le point
annoncé dans `structures-stacks-queues-001`, et il vaut la peine de l'écrire une fois pour le
constater soi-même.

**Équilibré ou dégénéré, la différence est de nature.** Un arbre binaire de recherche équilibré a une
hauteur en O(log n), donc une recherche en O(log n). Le même arbre construit à partir de données déjà
triées dégénère en liste chaînée : hauteur n, recherche O(n), et récursion de profondeur n. Les deux
structures ont le même type C# et des performances sans commune mesure. C'est pour cela que les
implémentations réelles s'auto-équilibrent, et que `SortedDictionary` garantit O(log n) là où un arbre
naïf ne garantit rien.

**Quand dérécursiver.** La récursion sur un arbre est sûre tant que la hauteur reste bornée par la
structure — un arbre équilibré de un million de nœuds a une hauteur d'environ vingt. Elle devient
dangereuse dès que l'arbre peut dégénérer, ou qu'il provient d'une source non maîtrisée : une
hiérarchie de commentaires importée peut être arbitrairement profonde. Dans ce cas, la pile explicite
n'est pas une optimisation, c'est une protection.

**Attention aux cycles.** Un arbre n'a pas de cycle par définition, mais une structure *décrite*
comme un arbre — un identifiant de parent en base, par exemple — peut en contenir un à cause d'une
donnée corrompue. Un parcours naïf tourne alors indéfiniment. Dès que la structure vient d'une source
externe, maintenez l'ensemble des nœuds déjà visités.

## Exemple commenté

```csharp
public sealed class TreeNode(string name)
{
    public string Name { get; } = name;

    public List<TreeNode> Children { get; } = [];
}

// Préfixe : le nœud est traité AVANT ses enfants. Ordre naturel pour un affichage indenté.
public static void PrintIndented(TreeNode node, int depth = 0)
{
    Console.WriteLine($"{new string(' ', depth * 2)}{node.Name}");
    foreach (TreeNode child in node.Children)
    {
        PrintIndented(child, depth + 1);
    }
}

// Suffixe : le nœud est traité APRÈS ses enfants, car il a besoin de leur résultat.
public static int CountNodes(TreeNode node)
{
    int total = 1;
    foreach (TreeNode child in node.Children)
    {
        total += CountNodes(child);
    }

    return total;
}
```

La version itérative, sûre quelle que soit la profondeur. Remplacer `Stack` par `Queue` change le
parcours en largeur — et rien d'autre :

```csharp
public static int CountNodesIterative(TreeNode root)
{
    ArgumentNullException.ThrowIfNull(root);

    var pending = new Stack<TreeNode>();       // Queue ici -> parcours en largeur.
    var visited = new HashSet<TreeNode>();     // Protection contre un cycle en données corrompues.
    pending.Push(root);
    int total = 0;

    while (pending.Count > 0)
    {
        TreeNode node = pending.Pop();
        if (!visited.Add(node))
        {
            continue;                          // Déjà vu : la structure n'est pas un arbre.
        }

        total++;
        foreach (TreeNode child in node.Children)
        {
            pending.Push(child);
        }
    }

    return total;
}
```

L'état vit désormais sur le tas plutôt que sur la pile d'appels : une hiérarchie de cent mille
niveaux ne pose plus de problème.

## Contre-exemple et erreur fréquente

```csharp
public static int Height(TreeNode node)
{
    if (node.Children.Count == 0)
    {
        return 0;                                  // Convention « arêtes »…
    }

    int max = 0;
    foreach (TreeNode child in node.Children)
    {
        max = Math.Max(max, Height(child));
    }

    return max + 1;
}

public static bool IsDeep(TreeNode node) => Height(node) >= node.Children.Count;
```

Trois défauts se superposent.

La convention n'est écrite nulle part : cette implémentation compte les **arêtes**, donc une feuille a
une hauteur de 0. Un appelant qui suppose la convention « nœuds » obtient systématiquement un de
moins, et l'écart passe inaperçu jusqu'à un cas limite.

`IsDeep` compare une hauteur à un nombre d'enfants : deux grandeurs sans rapport. Le code compile,
s'exécute, et son résultat n'a aucun sens métier. C'est le genre de ligne qu'une reformulation écrite
avant le code aurait empêchée.

Enfin, `node` n'est jamais vérifié : un enfant nul dans la collection produit une
`NullReferenceException` à la ligne `node.Children.Count`, à un endroit qui ne dit rien de l'origine
du problème. Et si l'arbre provient d'une base de données avec un cycle, la récursion sature la pile.

## Vérification de compréhension

Pour « calculer la taille totale d'un dossier » et « lister les dossiers par niveau », dites quel
parcours convient à chacun et pourquoi l'autre ne conviendrait pas.

:::quiz
id=structures-trees-001-check
question=Vous voulez calculer la taille cumulée de chaque dossier d'une arborescence. Quel parcours convient ?
option=Le parcours préfixe, qui traite le dossier avant son contenu
option=Le parcours suffixe, qui traite les enfants d'abord car le parent a besoin de leurs résultats
option=Le parcours en largeur, qui traite l'arborescence niveau par niveau
correct=1
success=Correct : une agrégation remonte l'information depuis les feuilles, donc le nœud ne peut être traité qu'après ses enfants.
retry=Relisez le tableau des quatre parcours : la question est de savoir si le parent a besoin du résultat de ses enfants pour se calculer.
:::

## Exercice guidé

Ouvrez `structures-tree-height-001` dans `/practice`, puis procédez ainsi.

1. Écrivez la convention de hauteur retenue — nœuds ou arêtes — dans le contrat, avant tout code.
2. Listez les cas : arbre nul, nœud unique, chaîne linéaire, arbre équilibré.
3. Implémentez en récursif, puis annoncez la profondeur maximale des appels.
4. Vérifiez que le cas « nœud unique » correspond bien à la convention annoncée.

## Exercice autonome

Écrivez une méthode qui retourne tous les nœuds situés à une profondeur donnée.

Décidez avant de coder : le parcours retenu et pourquoi, la convention de profondeur, le comportement
pour une profondeur supérieure à la hauteur, pour une profondeur négative, et l'ordre des nœuds
retournés. Précisez si cet ordre fait partie du contrat.

## Débogage

Un ticket indique : « L'affichage de l'arborescence des catégories fait tomber l'application, mais
seulement chez un client. »

1. **Symptôme** : arrêt brutal du processus, sans exception dans les journaux.
2. **Hypothèse** : un débordement de pile — soit une hiérarchie très profonde, soit un cycle dans les
   identifiants de parent.
3. **Preuve** : mesurez la profondeur maximale sur ce jeu de données avec un parcours itératif, et
   cherchez un nœud atteint deux fois. `StackOverflowException` ne peut pas être attrapée, d'où
   l'absence de trace.
4. **Prévention** : passez au parcours itératif avec un ensemble de nœuds visités, et ajoutez un test
   sur une hiérarchie contenant volontairement un cycle.

## Entretien

Question posée à voix haute : *quelle est la différence entre un parcours en profondeur et un
parcours en largeur, et quand choisissez-vous l'un plutôt que l'autre ?*

Une réponse solide cite la structure d'attente — pile contre file — comme seule différence de code,
puis donne un critère de choix : la largeur pour le plus court chemin en nombre d'étapes, la
profondeur quand la mémoire est contrainte ou qu'on cherche à explorer complètement une branche.

## Résumé

- Taille, profondeur et hauteur sont trois mesures distinctes ; la convention s'écrit.
- Préfixe pour copier, suffixe pour agréger et supprimer, infixe pour l'ordre, largeur pour la distance.
- Le code des parcours ne diffère que par la position du traitement du nœud.
- Pile ou file : le même parcours devient profondeur ou largeur.
- Un arbre dégénéré ramène une recherche en O(log n) à O(n).

## Cartes de révision

Question : quelle propriété rend le parcours infixe précieux sur un arbre binaire de recherche ?
Réponse attendue : il énumère les valeurs dans l'ordre croissant.

Question : quel garde-fou ajouter à un parcours d'arbre issu d'une base de données ? Réponse
attendue : un ensemble des nœuds déjà visités, car une donnée corrompue peut créer un cycle.

## Test de maîtrise

Sans relire, écrivez la version itérative d'un parcours en largeur qui retourne les noms des nœuds
niveau par niveau, groupés. Justifiez la structure d'attente, expliquez comment vous détectez le
changement de niveau, et indiquez ce que retourne votre méthode sur un arbre vide.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
