# Compter les segments de trace orphelins d'un journal

Implémentez `Submission.OrphanSpans` avec la signature fournie. Une trace distribuée raconte une
requête en segments : chaque service enregistre le sien et y note l'identifiant de son parent. Quand
la propagation du contexte se perd — un en-tête oublié, une file qui ne le transmet pas — le segment
arrive quand même au collecteur, mais **orphelin** : son parent n'existe nulle part, et la cascade
affichée s'arrête au milieu. Votre fonction mesure l'ampleur de cette perte dans un journal.

## Le format du journal

Des segments `identifiant>parent` séparés par des points-virgules. Un parent noté `-` déclare une
racine : le début assumé d'une requête. L'ordre des segments est quelconque — un collecteur reçoit
dans l'ordre d'arrivée réseau, pas dans l'ordre de causalité.

## Ce qu'il faut produire

Le nombre de segments orphelins : ceux dont le parent n'est ni le tiret de racine, ni un identifiant
présent dans le journal. Un segment qui se déclare **son propre parent** est orphelin aussi — un
cycle d'un seul nœud ne raccroche rien.

```text
OrphanSpans("a1>-;b2>a1;c3>a1")                       →  0
OrphanSpans("a1>-;b2>zz")                             →  1
OrphanSpans("root>-;child>root;ghost>lost;stray>gone") →  2
```

## Le piège de l'ordre

Un enfant peut arriver avant son parent : `b2>a1;a1>-` est un journal parfaitement recousu. Juger les
parents pendant la première lecture condamnerait tous les enfants précoces ; il faut connaître tous
les identifiants avant de juger le moindre lien.

## Les refus

`ArgumentException` pour un journal vide, un segment sans séparateur ou aux identifiants vides, et
pour un identifiant de segment répété — deux segments sous le même nom rendent le journal impossible
à recoudre.

## Avant d'écrire

Prédisez le compte pour deux segments qui se citent mutuellement sans racine, et pour un journal d'une
seule racine. Dites ce que le premier cas révèle que le comptage d'orphelins ne voit pas.
