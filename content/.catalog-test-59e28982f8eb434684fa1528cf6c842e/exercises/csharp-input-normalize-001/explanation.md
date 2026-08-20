# Explication

Une ligne de code utile, et pourtant un vrai contrat : cet exercice apprend à lire ce que `Trim`
fait *exactement*, et à distinguer les états d'une chaîne que le langage confond volontiers.

`Trim` retire les caractères blancs aux deux extrémités — espaces, tabulations, retours à la
ligne, et plus généralement tout ce qu'Unicode classe comme espace — et ne touche à rien
d'autre. Le contenu intérieur est préservé tel quel : `"Ada  Lovelace"` garde son double espace
central, la casse n'est pas modifiée, aucun accent n'est transformé. Cette précision compte
parce que « normaliser » est un mot élastique : la version qui en profite pour compacter les
espaces internes ou changer la casse fait *plus* que le contrat, et l'excès de zèle est un bug
au même titre que le manque — l'appelant qui voulait préserver un nom composé retrouve ses
données altérées. Les cas cachés placent des blancs intérieurs précisément pour réfuter les
normalisations trop enthousiastes, et des tabulations en bord pour vérifier que le nettoyage ne
se limite pas au caractère espace.

Le régime d'erreur mérite sa ligne. `null` lève `ArgumentNullException` : une chaîne absente
n'est pas une chaîne vide, et la traiter comme telle masquerait un défaut de l'appelant — la
distinction est la même que pour les collections, et elle structure tout le catalogue. En
revanche, la chaîne vide et la chaîne toute blanche sont des entrées *valides* : la première
traverse inchangée, la seconde devient vide après rognage. Trois états — absent, vide, blanc —
trois comportements distincts, et c'est exactement ce que `IsNullOrWhiteSpace` aurait aplati en
un seul si on l'avait utilisé ici. Le choix de l'outil de garde découle du contrat, jamais de
l'habitude.

Une propriété discrète vaut d'être nommée : la fonction est *idempotente*. L'appliquer deux fois
donne le même résultat qu'une fois, puisqu'une chaîne déjà rognée n'a plus de blancs de bord.
L'idempotence est ce qui rend une normalisation composable — on peut l'appliquer « au cas où »
à chaque frontière du système sans craindre d'effet cumulatif — et vérifier cette propriété est
un excellent test à écrire soi-même.

Le coût est linéaire dans la longueur, avec au plus une allocation — `Trim` retourne la même
instance quand il n'y a rien à retirer, détail d'implémentation agréable et non contractuel.

La transposition : toute donnée saisie franchit une frontière — formulaire, fichier, API — et la
frontière est l'endroit où rogner, une fois, systématiquement. Les doublons « invisibles » dans
les bases, les recherches qui ne trouvent pas ce qui existe, les clés en double dans les
imports : une part remarquable de ces tickets se résout par ce `Trim` de frontière appliqué au
bon moment.
