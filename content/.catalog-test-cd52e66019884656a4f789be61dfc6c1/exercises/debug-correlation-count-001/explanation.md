# Explication

Compter les apparitions d'un identifiant de corrélation dans un journal : c'est le geste par
lequel on estime la trace d'une requête — combien d'étapes ont journalisé cet identifiant ? —
et sa mécanique de recherche répétée contient deux décisions qui changent le compte.

La première est le point de reprise. Après une occurrence trouvée en `index`, la recherche
suivante repart de `index + correlationId.Length` — *après* l'occurrence entière — et non de
`index + 1`. La différence définit ce qu'on compte : avec le saut complet, les occurrences sont
disjointes, sans recouvrement ; avec le pas de un, un motif auto-chevauchant — `aa` dans
`aaa` — compterait deux au lieu d'un. Pour des identifiants de corrélation, le compte disjoint
est le seul qui corresponde à la question posée — chaque occurrence est une écriture de journal
distincte — et le contrat le fixe. Mais il faut savoir que l'autre convention existe, car
l'écart entre les deux ne se voit que sur des motifs répétitifs, celles précisément qu'un
identifiant mal choisi peut produire.

La deuxième est la comparaison : `StringComparison.Ordinal`, binaire, sensible à la casse. Un
identifiant de corrélation est une clé technique — généré par une machine, recopié par des
machines — et le comparer culturellement serait à la fois faux et lent : les règles culturelles
peuvent fusionner ou distinguer des séquences selon la machine, et le journal doit se dépouiller
pareil partout. La règle est générale : les chaînes techniques se comparent en ordinal,
la comparaison culturelle se réserve au texte humain. Passer le comparateur *explicitement*,
même quand le défaut conviendrait, documente que la question a été posée.

La boucle elle-même est le motif de balayage par `IndexOf` : chercher à partir d'un point,
traiter, avancer le point. Sa condition d'arrêt est le moins un de l'absence, et sa progression
stricte — le point avance d'au moins la longueur du motif à chaque tour — garantit la
terminaison. Les gardes d'entrée règlent les vides : un journal vide n'a aucune occurrence, un
identifiant vide n'est pas un motif cherchable — zéro dans les deux cas, convention de comptage
plutôt qu'exception, cohérente avec un outil de dépouillement qui doit digérer des entrées
imparfaites sans s'arrêter.

Les cas cachés visent les trois axes : la casse — l'identifiant en majuscules ne compte pas
pour son jumeau minuscule —, le motif auto-chevauchant qui départage les points de reprise, et
l'absence totale qui rend zéro.

Le coût est linéaire en pratique dans la taille du journal. La transposition est l'outillage
de diagnostic entier : compter des marqueurs, des codes d'erreur, des débuts de transaction —
chaque comptage textuel pose les deux mêmes questions, recouvrement et comparateur, avant
d'écrire la boucle.
