# Explication

Cet histogramme de mots ressemble au comptage de mots voisin et s'en distingue par deux choix de
conception opposés — et c'est la comparaison des deux exercices qui enseigne le plus.

Premier choix : où loger l'insensibilité à la casse. L'exercice voisin normalise les *clés* en
minuscules ; celui-ci laisse les clés telles quelles et confie la fusion au *comparateur* du
dictionnaire — `StringComparer.OrdinalIgnoreCase`, passé au constructeur. Les deux stratégies
cumulent `Chat` et `chat` dans la même entrée ; elles diffèrent par ce qui reste visible. Avec
le comparateur, la clé stockée est la *première graphie rencontrée* — l'histogramme de
`"Chat chat"` porte la clé `Chat` — et les données d'origine survivent ; avec la normalisation,
toutes les clés sont en minuscules, uniformes mais appauvries. Aucun des deux n'est supérieur
dans l'absolu : le comparateur préserve l'information, la normalisation garantit une forme de
sortie canonique. Ce qu'il faut retenir, c'est que l'identité des clés d'un dictionnaire est
*configurable*, que ce choix est une clause de contrat, et que les cas de test doivent le figer
— ici, les cachés mêlent les casses et vérifient le cumul.

Deuxième choix : la définition du mot. Ici, le séparateur est l'espace, point final — c'est un
découpage par `Split`, avec `RemoveEmptyEntries` pour absorber les espaces répétés et les bords.
L'exercice voisin définissait le mot par son alphabet et exigeait un automate ; celui-ci définit
le mot par son séparateur et s'offre la solution d'une ligne. La ponctuation collée reste donc
collée — `chat,` est un mot distinct de `chat` — et c'est conforme au contrat, qui ne promet
rien sur la ponctuation. Savoir lire ce que l'énoncé *ne dit pas* et ne pas le sur-implémenter
est une discipline : chaque promesse non demandée est du comportement à maintenir.

Le cumul reprend le motif de lecture-écriture en deux temps — `TryGetValue` pour le compte
courant, réécriture incrémentée — une interrogation de table par mot, l'idiome exact du
comptage. L'entrée absente, vide ou blanche rend un dictionnaire vide *du bon comparateur* :
même vide, l'objet rendu porte déjà sa règle d'identité, et un appelant qui y ajoute ensuite
des clés bénéficie de la fusion de casse. C'est un détail qui montre où vit vraiment le
contrat : dans l'objet construit, pas dans les données qui y transitent.

Le coût est linéaire, allocations de `Split` comprises — pour un histogramme sans allocation
de segments, l'automate du voisin s'adapterait. La transposition : compteurs d'événements par
code, agrégats par référence produit, tallies de votes — et à chaque fois la même première
question, celle du comparateur : qu'est-ce qui fait que deux clés sont la même clé ?
