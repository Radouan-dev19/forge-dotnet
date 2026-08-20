# Inspecter les données sans les modifier

## Objectif observable

À la fin de cette leçon, vous saurez observer l'état d'un programme suspendu sans altérer ce que vous
observez, et vous saurez tenir un journal de bug en six champs qui transforme un diagnostic en preuve
reproductible.

## Prérequis

- Avoir lu `debug-stacktraces-breakpoints-001` et savoir poser un point d'arrêt conditionnel.
- Savoir ouvrir les fenêtres de variables locales et d'espion de votre environnement.

## Intuition

Quand le programme est suspendu, la tentation est de « voir ce qui se passe si » : changer une valeur
à la main, appeler une méthode depuis la fenêtre d'observation. C'est exactement ce qu'il ne faut pas
faire.

Une observation qui modifie l'état détruit la seule chose qui avait de la valeur : la reproduction du
défaut. Vous obtenez alors un état qui n'existe dans aucune exécution réelle, et le symptôme disparaît
sans que personne ne sache pourquoi.

## Explication

**Les fenêtres d'inspection ne sont pas équivalentes.** *Locals* affiche automatiquement les variables
du cadre courant : c'est ce qu'on lit en premier, sans rien taper. *Watch* évalue des expressions que
vous écrivez : c'est là que le danger commence. *Autos* montre les variables impliquées dans les
lignes voisines. *Immediate* exécute du code arbitraire dans le contexte suspendu — l'outil le plus
puissant et le plus destructeur.

**Une expression d'observation peut avoir des effets de bord.** Écrire `list.First()` dans la fenêtre
d'espion sur une séquence différée déclenche une énumération réelle. Sur un `IEnumerable` non
rejouable — un flux, un lecteur de fichier — cette énumération **consomme** des éléments que le
programme ne verra jamais. Vous avez modifié le comportement en l'observant.

Le même piège existe avec toute propriété dont l'accesseur fait plus que retourner un champ : un
compteur qui s'incrémente, un chargement paresseux qui déclenche une requête, un cache qui se
remplit. La règle : dans la fenêtre d'espion, n'écrivez que des expressions dont vous savez qu'elles
ne font que lire. Les environnements modernes proposent d'ailleurs un marqueur explicite pour les
évaluations à effet de bord — s'il apparaît, c'est un avertissement, pas une formalité.

**Comparer attendu et réel, dans cet ordre.** L'observation utile n'est pas « quelle est la valeur ? »
mais « la valeur correspond-elle à ce que j'avais prédit ? ». Prédire **avant** de regarder est ce qui
transforme l'inspection en expérience. Si vous n'aviez pas de prédiction, vous ne pouvez rien
conclure : toute valeur observée paraîtra plausible après coup.

**Observer la donnée, pas seulement la variable.** Une référence identique ne signifie pas un contenu
identique, et deux collections de même taille peuvent différer sur un seul élément. Sur une
collection volumineuse, l'inspection visuelle ne tient pas : le geste efficace est de comparer une
projection — le nombre d'éléments, une somme, l'ensemble des clés distinctes — plutôt que de faire
défiler mille lignes.

**Le journal de bug en six champs.** C'est le livrable du débogage, et il vaut plus que la correction
elle-même.

*Symptôme* : ce qui est observable, avec les valeurs exactes attendues et obtenues. *Contexte* : les
données et la configuration qui reproduisent. *Hypothèse* : une phrase falsifiable. *Preuve* :
l'observation qui confirme ou réfute, avec l'endroit où elle a été faite. *Cause* : le mécanisme, pas
la ligne. *Correction et test* : ce qui change, et le test qui échouait avant.

Le champ le plus souvent bâclé est *Cause*. Écrire « la ligne 42 plantait » n'est pas une cause :
c'est une répétition du symptôme. Une cause s'énonce comme un mécanisme — « la collection est
partagée entre deux appels et le second la modifie pendant que le premier l'énumère ».

**Le test de non-régression est ce qui clôt le diagnostic.** Un bug corrigé sans test qui échouait
avant n'est pas corrigé : il est déplacé. La preuve que vous avez compris la cause, c'est votre
capacité à écrire un test qui la réveille à volonté.

## Exemple commenté

Le défaut : un rapport affiche parfois un total faux. Prédiction écrite avant d'ouvrir le débogueur —
*« la collection reçue contient 12 lignes et leur somme vaut 340,50 »*.

```csharp
public decimal ComputeReport(IEnumerable<OrderLine> lines)
{
    // Point d'arrêt ici. Dans Locals on lit le type réel de `lines` :
    // s'il s'agit d'une requête différée et non d'une liste, tout ce qui suit est suspect.
    decimal total = lines.Sum(line => line.Amount);
    int count = lines.Count();          // Second parcours de la même source.

    return count == 0 ? 0m : total;
}
```

Ce qu'on observe, sans rien modifier : `lines` est de type `WhereSelectEnumerableIterator`, donc une
requête différée. `total` vaut 340,50 comme prévu, mais `count` vaut 0 — la source n'était pas
rejouable et le premier parcours l'a consommée.

L'observation décisive coûte une seule expression **en lecture seule**, posée avant l'appel : le type
concret de `lines`. Écrire `lines.Count()` dans la fenêtre d'espion aurait, lui, consommé la séquence
et fait disparaître le symptôme — c'est précisément le piège de cette leçon.

Le journal correspondant :

```text
Symptôme   : total 340,50 affiché avec un compte de 0 lignes ; attendu 12 lignes.
Contexte   : appelant passant une requête différée issue d'un lecteur de flux.
Hypothèse  : la source n'est pas rejouable et le second parcours ne voit plus rien.
Preuve     : type concret observé dans Locals = itérateur différé ; count = 0 après Sum.
Cause      : la méthode énumère deux fois une séquence dont le contrat ne garantit qu'un parcours.
Correction : matérialiser une fois en début de méthode ; test avec une source à parcours unique.
```

## Contre-exemple et erreur fréquente

Session de débogage typique, et destructrice :

```csharp
public void Apply(List<Discount> discounts, Order order)
{
    // Suspendu ici, le développeur tape dans la fenêtre Immediate :
    //     discounts.RemoveAll(d => d.Expired)      -> modifie la collection observée
    //     order.Recalculate()                      -> change l'état de l'objet
    //     discounts.First().Rate = 0.2m            -> écrit dans la donnée
    foreach (Discount discount in discounts)
    {
        order.Apply(discount);
    }
}
```

Chacune de ces trois expressions a modifié le programme. La première a supprimé des éléments que la
boucle aurait dû traiter — et invalide au passage l'énumérateur si la boucle avait déjà commencé. La
deuxième a déclenché un recalcul qui n'aurait pas eu lieu à cet instant. La troisième a écrasé une
donnée métier.

Le symptôme change ou disparaît, et le développeur conclut que « ça marche maintenant ». Il vient en
réalité de rendre le défaut non reproductible, ce qui est strictement pire que la situation de
départ : la prochaine occurrence sera en production, sans reproduction locale.

La discipline est simple. Dans un contexte suspendu, on **lit**. Si l'on veut tester une hypothèse
qui demande un état différent, on sort du débogueur et on écrit un test — qui, lui, est reproductible
et restera dans le dépôt.

## Vérification de compréhension

Citez deux expressions apparemment inoffensives qui, écrites dans une fenêtre d'espion, modifient
l'état du programme observé.

:::quiz
id=debug-data-inspection-001-check
question=Pourquoi éviter d'écrire une expression comme `items.Count()` dans la fenêtre d'espion sur une séquence différée ?
option=Parce que l'évaluation est trop lente et fait expirer le débogueur
option=Parce qu'elle déclenche une énumération réelle qui peut consommer une source à parcours unique et faire disparaître le symptôme
option=Parce que le débogueur refuse d'évaluer les méthodes d'extension
correct=1
success=Correct : observer ne doit rien changer. Sur une source non rejouable, l'évaluation consomme des éléments que le programme ne verra jamais.
retry=Relisez le passage sur les expressions d'observation à effet de bord, et ce qui distingue une lecture d'une énumération.
:::

## Exercice guidé

Ouvrez `debug-distinct-events-001` dans `/practice`, puis procédez ainsi.

1. Écrivez votre prédiction — valeurs attendues — **avant** de poser le point d'arrêt.
2. Observez uniquement dans la fenêtre des variables locales, sans rien taper dans l'espion.
3. Remplissez les six champs du journal, en veillant à ce que *Cause* décrive un mécanisme.
4. Écrivez le test qui échoue avant correction, puis corrigez.

Le DebugLab `debug-data-mutation-001` propose le même protocole sur un dépôt cassé complet.

## Exercice autonome

Un traitement produit un résultat correct au premier appel et faux au second, dans le même processus.

Écrivez, sans code : trois hypothèses classées par probabilité, pour chacune l'observation en lecture
seule qui la réfuterait, et l'endroit exact où vous la feriez. Précisez ce que vous refusez d'évaluer
dans la fenêtre d'espion et pourquoi.

## Débogage

Un ticket indique : « Le bug disparaît dès qu'on ouvre le débogueur. »

1. **Symptôme** : le comportement diffère entre exécution libre et exécution suspendue.
2. **Hypothèse** : soit une expression d'observation modifie l'état, soit le défaut dépend du temps
   ou de la concurrence, que la suspension modifie mécaniquement.
3. **Preuve** : videz toutes les expressions d'espion et rejouez. Si le défaut réapparaît, c'était
   l'observation ; s'il persiste, c'est la temporalité, et le débogueur n'est pas le bon outil —
   passez à la journalisation.
4. **Prévention** : consignez dans le journal quelles expressions étaient évaluées, et privilégiez les
   points de trace, qui écrivent sans suspendre.

## Entretien

Question posée à voix haute : *comment savez-vous que vous avez trouvé la cause d'un bug, et pas
seulement fait disparaître le symptôme ?*

Une réponse solide donne un critère opposable : je sais écrire un test qui échoue avant la correction
et réussit après, et je sais énoncer le mécanisme sans citer un numéro de ligne. Elle cite un cas où
elle a d'abord corrigé le symptôme et ce que cela a coûté.

## Résumé

- Observer ne doit jamais modifier ce qu'on observe.
- Une expression d'espion peut énumérer, charger ou compter : ce sont des effets de bord.
- Prédire avant de regarder transforme l'inspection en expérience.
- La cause est un mécanisme, pas un numéro de ligne.
- Sans test qui échouait avant, le bug est déplacé, pas corrigé.

## Cartes de révision

Question : que faire lorsqu'une hypothèse exige de modifier l'état pour être testée ? Réponse
attendue : sortir du débogueur et écrire un test, qui sera reproductible et conservé.

Question : lequel des six champs du journal est le plus souvent bâclé, et à quoi le reconnaît-on ?
Réponse attendue : la cause, quand elle se contente de répéter le symptôme ou de citer une ligne.

## Test de maîtrise

Sans relire, décrivez le protocole complet pour diagnostiquer un total erroné dans un rapport
agrégé : prédiction, endroit d'observation, expressions autorisées, expressions refusées, les six
champs du journal, et le test de non-régression.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
