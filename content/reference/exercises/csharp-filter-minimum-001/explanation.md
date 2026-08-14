# Explication

Le filtrage est l'opération de collection la plus fréquente du métier, et cet exercice en fixe
les trois clauses de contrat que l'on croit toujours évidentes jusqu'au jour où deux développeurs
les ont comprises différemment.

Première clause : la borne est *incluse*. « Au moins le minimum » se traduit par `>=`, et la
valeur exactement égale au seuil traverse le filtre. Un `>` à la place éliminerait silencieusement
les éléments posés sur la borne — sur des seuils de prix ou de stock, c'est un désaccord
commercial déguisé en bug. Le cas caché posé exactement sur le seuil départage les deux
écritures ; c'est le test le moins cher et le plus rentable de tout le domaine des filtres.

Deuxième clause : l'ordre d'entrée est *préservé*. `Where` parcourt la source dans l'ordre et
émet les survivants dans ce même ordre — rien n'est trié, rien n'est regroupé. Cette stabilité
paraît gratuite ici ; elle devient un engagement dès que l'appelant pagine ou compare deux
appels. Un filtre implémenté par une structure intermédiaire qui perd l'ordre — un ensemble, par
exemple — rendrait les mêmes éléments dans un ordre imprévisible, et les cas cachés, qui fixent
la sortie attendue élément par élément, le réfutent.

Troisième clause : la sortie est une collection *neuve*. La chaîne `Where(...).ToArray()` lit la
source sans jamais l'écrire et matérialise le résultat une fois, en fin de chaîne. L'entrée
reste intacte — le harnais le vérifie — et l'appelant reçoit un tableau indépendant, qu'il peut
modifier sans effet retour. Retourner l'énumérable paresseux sans `ToArray` serait un autre
contrat : chaque parcours rejouerait le filtre, et une modification ultérieure de la source
changerait rétroactivement le résultat observé. Pour une méthode qui promet un tableau, figer
est la seule lecture honnête de la signature.

Le squelette d'erreur ne varie pas : `null` signale une faute d'appel, le tableau vide traverse
et rend un tableau vide, un filtre qui ne retient rien rend un tableau vide lui aussi — deux
chemins différents vers la même sortie, tous deux couverts. Le minimum peut être négatif, et le
prédicat n'a pas à s'en soucier : la comparaison est définie sur tout le domaine.

Le coût est linéaire avec une allocation finale ; en boucle manuelle, il faudrait soit deux
passes — compter puis remplir — soit une liste croissante : `Where` encapsule ce choix et le
rend indifférent à l'appelant.

La transposition : tout filtre à seuil — âge, montant, priorité — se spécifie en répondant à ces
trois clauses. Borne incluse ou non, ordre garanti ou non, source intacte ou non. Trois phrases
dans le contrat, trois cas dans les tests, et le filtre cesse d'être une source de litiges.
