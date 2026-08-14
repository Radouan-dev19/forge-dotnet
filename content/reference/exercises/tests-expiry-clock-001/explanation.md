# Explication

Une comparaison de dates d'une ligne — et le titre parle de *fixer le temps dans un test* :
tout l'exercice tient dans ce que la signature ne contient pas. Il n'y a aucun appel d'horloge
dans la règle, et cette absence est la technique enseignée.

L'énoncé demande ce qu'une horloge lue dans la règle ferait à un test exécuté un autre jour :
elle le ferait *mentir*. Un test écrit lundi avec une échéance à mardi passe lundi et échoue
mercredi — sans qu'une ligne de code ait changé. Ces tests dépendants du calendrier sont une
plaie reconnaissable : ils cassent par vagues au passage des minuits, des fins de mois, des
changements d'heure, et chaque cassure consomme une enquête pour conclure « c'est juste la
date ». La cause racine est toujours la même — un appel à l'horloge système enfoui *dans* la
règle métier — et le remède est structurel : le temps entre par la porte, en paramètre. La
règle devient une fonction pure de deux dates, et le test choisit *son* aujourd'hui : les trois
cas de l'énoncé — avant, après, le jour même — s'écrivent avec des dates littérales et
passeront dans dix ans.

La règle elle-même mérite sa précision : `expiresOn < today` — expiré signifie que l'échéance
est *strictement avant* aujourd'hui, donc le jour de l'échéance, on n'est *pas encore* expiré.
C'est la convention « valable jusqu'à ce jour inclus », la plus courante pour les dates
d'expiration commerciales, et l'exemple de l'énoncé la confirme. La convention inverse existe
— expiré dès le jour même — et un seul caractère les sépare : le cas posé exactement sur
l'échéance est donc le verrou du contrat, la valeur de frontière par excellence de ce domaine.

`DateOnly` est le bon type, sans heure ni fuseau : la question est calendaire, et embarquer un
horodatage complet rouvrirait les ambiguïtés de minuit que le type évite par construction.

Dans une application réelle, quelqu'un doit bien lire l'horloge : la réponse mûre est de le
faire *une fois, au bord* — le contrôleur, le travailleur planifié — ou derrière une
abstraction d'horloge injectée, et de passer la date à toutes les règles. Le cœur du domaine
reste pur et testable par table ; seul le bord touche au temps réel.

Les cas cachés déclinent le triplet de frontière autour de l'échéance. Le coût est une
comparaison. La transposition est le réflexe à généraliser : traquer les lectures d'horloge,
d'aléa et d'environnement au milieu des règles — les trois mêmes ennemis du déterminisme — et
les remonter en paramètres ou en dépendances explicites. Une règle qui reçoit son monde est
une règle qu'on peut prouver.
