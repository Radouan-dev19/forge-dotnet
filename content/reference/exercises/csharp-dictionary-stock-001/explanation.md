# Explication

La fusion de deux stocks a l'air d'une boucle d'additions ; la solution s'organise pourtant en
trois temps — valider, puis verser, puis verser encore — et cet ordre est la réponse à la
question que l'énoncé pose explicitement : comment empêcher une validation tardive de produire
un résultat partiel ?

Si les quantités négatives étaient contrôlées *pendant* la fusion, l'exception pourrait tomber
au milieu du second dictionnaire : le résultat contiendrait déjà la moitié des additions.
Retourné, ce fragment serait un mensonge ; même simplement construit, il représente du travail
fait sur des données qu'on allait refuser. Valider les deux sources *entièrement* avant la
première écriture donne à la méthode une propriété qui a un nom : l'atomicité par validation
préalable — soit tout le travail se fait, soit rien ne commence. C'est le même raisonnement qui
fait vérifier un fichier entier avant de l'importer, ou un lot de commandes avant d'en insérer
une seule.

Le deuxième sujet est l'identité des clés. Le contrat compare les références sans tenir compte
de la casse et impose des clés de sortie en minuscules invariantes : la solution normalise par
`ToLowerInvariant()` au moment du versement, si bien que `STYLO` et `stylo` fusionnent en une
seule entrée. Le dictionnaire résultat est créé avec `StringComparer.Ordinal` — une fois les
clés normalisées, la comparaison binaire suffit et ne réserve aucune surprise culturelle. On
aurait pu créer le dictionnaire avec un comparateur insensible à la casse et garder les clés
d'origine ; mais alors *laquelle* des casses d'origine survivrait dépendrait de l'ordre de
parcours, et le contrat exige une forme de sortie déterministe. Normaliser la donnée plutôt que
la comparaison est ici le choix qui rend le résultat indépendant de l'ordre d'arrivée.

Le squelette de versement mérite un mot : `TryGetValue` puis réécriture additionnée, le motif
« lire le cumul courant, défaut zéro, écrire la somme ». La méthode privée `MergeInto` l'écrit
une fois et sert deux fois — la répéter en ligne serait la première étape vers deux versions qui
divergent.

Les entrées, elles, ne sont jamais modifiées : tout se verse dans un dictionnaire neuf, et les
cas du harnais qui capturent les arguments le vérifient. Les cas cachés croisent les casses,
posent la clé commune aux deux sources — la somme doit apparaître — et glissent la quantité
négative qui doit tout refuser avant toute écriture.

Le coût est linéaire en le nombre d'entrées des deux sources. La transposition : réconcilier
deux inventaires, deux relevés, deux caches — toujours valider d'abord, normaliser l'identité
ensuite, cumuler enfin, et rendre du neuf.
