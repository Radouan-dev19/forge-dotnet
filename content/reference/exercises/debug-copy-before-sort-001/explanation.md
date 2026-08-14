# Explication

Cet exercice rejoue, côté débogage, une scène que tout développeur finit par vivre : une fenêtre
d'observation — un `Watch`, un point d'arrêt, un journal — montre des données *déjà triées*
alors que le code sous enquête est censé les recevoir brutes. Le suspect n'est pas le code
observé : c'est un tri en place exécuté en amont, souvent « juste pour afficher », qui a modifié
la réalité qu'on croyait regarder.

Le mécanisme tient en une signature trompeuse : `Array.Sort(array)` ne retourne rien — il trie
*en place*, dans le tableau qu'on lui donne. Un appelant qui voulait une vue triée et a écrit
`Array.Sort(values)` avant de la parcourir a silencieusement détruit l'ordre d'origine — celui
qui portait peut-être l'information du bug : l'ordre d'arrivée des événements, la séquence des
insertions. La solution sépare donc les deux mondes en deux lignes ordonnées : `Clone()`
d'abord, `Sort` sur la copie ensuite. L'ordre est essentiel — trier puis copier fabriquerait
deux tableaux triés, l'original compris, exactement le bug que l'exercice corrige. Le harnais
vérifie la promesse comme toujours : les arguments capturés avant l'appel doivent ressortir
identiques.

La leçon a un nom en conception d'interface : les opérations *mutantes* et les opérations
*productrices* doivent être distinguables à la lecture. La bibliothèque en offre les deux
formes — `Array.Sort` mute, `values.OrderBy(...).ToArray()` produit — et le choix entre elles
est un choix de propriété des données : qui possède ce tableau, qui a le droit de le
réordonner ? Dans un contexte de diagnostic, la réponse est toujours « personne » : les données
observées sont des pièces à conviction, et on ne retouche pas une pièce à conviction — on en
tire une copie de travail. C'est le sens du titre de l'énoncé, préserver les données observées.

Les décisions périphériques suivent le régime commun : `null` est une faute d'appel — pas de
données n'est pas des données vides —, le tableau vide rend un tableau vide neuf, les doublons
et les négatifs traversent le tri sans clause spéciale, et les cas cachés mêlent tout cela avec
une disposition qui réfute la sortie recopiée.

Le coût est celui du tri — n log n — plus une copie linéaire : la copie est marginale, et c'est
pour cela que « copier d'abord » ne se discute presque jamais en pratique.

La transposition dépasse le tri : `Reverse`, les écritures dans une liste reçue, la
normalisation « au passage » d'une collection partagée — chaque mutation d'une entrée est un
effet de bord qui traverse la frontière de la fonction. Le réflexe est toujours le même :
identifier qui d'autre voit ces données, et si la réponse n'est pas « personne », travailler
sur copie.
