# Explication

La fonction est une cascade de quatre retours. Ce qu'elle enseigne tient dans l'**ordre** de ces
retours, pas dans leur contenu.

**Le partage est une contrainte, pas une préférence.** Rebaser ou écraser une branche, c'est
fabriquer de nouveaux commits et abandonner les anciens. Tant que la branche n'existe que sur votre
poste, personne ne s'en aperçoit. Dès qu'un collègue l'a récupérée, les commits d'origine vivent chez
lui : votre réécriture ne les efface pas, elle crée des jumeaux. À la prochaine synchronisation, il
récupère les nouveaux, garde les anciens, et se retrouve avec chaque modification en double et des
conflits que personne n'a introduits. C'est pourquoi le partage se teste **en premier** : placé
ailleurs dans la cascade, une branche partagée mais bruitée serait écrasée, et la règle serait
violée exactement dans le cas où elle protégeait le plus.

Cette structure — la contrainte d'abord, les préférences ensuite — se retrouve partout où plusieurs
règles se disputent une décision. Une condition qui **interdit** ne se place jamais après une
condition qui **optimise**.

**L'écrasement est un arbitrage, pas un nettoyage.** Une branche dont l'histoire est faite de `wip`,
`fix typo` et `retry` raconte comment vous avez cherché, pas ce que vous avez trouvé. Personne ne
relira ces commits, et ils encombreront les recherches d'historique pendant des années. L'écrasement
garde le résultat et jette le cheminement. Mais il le jette **définitivement** : si vos commits
racontaient une progression utile — une correction, puis sa généralisation, puis son test — les
écraser détruit une information qu'un lecteur futur aurait aimé avoir. Le critère n'est donc pas la
longueur de l'histoire mais sa lisibilité.

**Le rebasage et l'avance rapide sont la même intention à deux états près.** Les deux visent une
histoire linéaire, sans commit de fusion. Quand la cible n'a pas bougé, il n'y a rien à rejouer :
l'avance rapide déplace simplement le pointeur. Quand elle a avancé, le rebasage rejoue vos commits
par-dessus les siens. Distinguer les deux évite un commit de fusion vide, ces lignes « Merge branch
main into main » qui ne disent rien et que l'on voit dans tant d'historiques.

**Ce que le modèle simplifie**, et qu'il faut savoir nommer en entretien : la propreté d'une histoire
n'est pas un booléen, et le partage non plus — une branche poussée sur un dépôt distant que personne
n'a récupérée est techniquement partagée sans l'être vraiment. Le modèle force une décision là où la
réalité offre des nuances, et c'est acceptable tant qu'on sait dans quel sens il se trompe : il
interdit parfois une réécriture qui aurait été sans danger. Une règle qui protège trop coûte moins
cher qu'une règle qui protège trop peu.

Le coût est constant : trois tests booléens, aucune allocation.
