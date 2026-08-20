# Explication

Une suite verte est un fait ; une suite utile est une hypothèse, et le test par mutation est le moyen
le moins cher de la vérifier. L'idée renverse la charge de la preuve : au lieu de demander « mes tests
passent-ils ? », on demande « si le code devenait faux, mes tests le verraient-ils ? ». On fabrique
donc des versions délibérément fausses de la règle, les mutants, et on compte ceux que la suite ne
distingue pas de l'original. Un survivant est un défaut que personne ne verrait passer en revue de
code, puisqu'il ne casse rien de ce qui est mesuré.

**Pourquoi chaque mutant se réduit à une seule valeur.** Durcir ou élargir une borne d'un intervalle
fermé ne change la réponse de la règle que sur la valeur immédiatement concernée : durcir la borne
basse ne rejette que la borne elle-même, l'élargir n'accepte que la valeur juste en dessous. Tout le
problème se ramène donc à un test d'appartenance : le mutant meurt si sa valeur distinctive figure
parmi les sondes, il survit sinon. C'est ce qui rend le score calculable sans exécuter le moindre
test : la structure de la règle suffit. C'est aussi ce qui explique le résultat le plus contre-intuitif
de l'exercice : une suite de cinquante valeurs du milieu obtient le même score qu'une suite vide,
puisque aucune de ces valeurs n'est distinctive pour aucun mutant.

**Le mutant équivalent est la subtilité qui sépare l'outil du gadget.** Quand une borne touche déjà la
limite du type, l'élargir ne produit pas une règle différente : la valeur qui départagerait l'original
et le mutant n'existe pas dans le domaine d'entrée. Compter ce mutant comme survivant serait doublement
faux — la suite n'a rien raté, et aucune sonde n'aurait pu le tuer. Les vrais outils de mutation
passent une part importante de leur temps précisément là : trier les survivants réels des équivalents.
L'exercice en donne la version décidable, celle où l'équivalence se démontre.

**Le débordement guette le calcul des voisins.** Produire la valeur d'en dessous de la plus petite
valeur du type ne lève pas d'erreur en arithmétique entière : le calcul s'enroule et livre la plus
grande valeur du type. Le comptage croirait alors chercher une sonde sous la borne basse en la
cherchant en réalité tout en haut du domaine. Comparer avant de calculer, ou calculer dans un type
plus large, sont les deux parades ; les deux demandent d'avoir vu le piège.

**Deux bornes confondues gardent quatre mutants.** L'intervalle réduit à une valeur a toujours deux
bornes, donc deux mutants durcissants — qui partagent la même valeur distinctive — et deux mutants
élargissants. Une même sonde peut en tuer deux d'un coup, mais fusionner les mutants fausserait le
score des suites partielles.

La transposition professionnelle est directe : avant de faire confiance à une suite héritée, mesurer
ce qu'elle tue coûte quelques minutes et évite de refactorer sous la protection d'un filet troué.
