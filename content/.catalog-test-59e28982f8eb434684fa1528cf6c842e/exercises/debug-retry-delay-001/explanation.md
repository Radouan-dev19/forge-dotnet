# Explication

Le délai entre deux tentatives est la politique la plus rejouée de l'informatique distribuée :
réessayer immédiatement aggrave la panne qu'on veut absorber, réessayer à intervalle fixe
synchronise tous les clients sur le même rythme, et la réponse standard est la croissance
exponentielle — doubler à chaque échec — *bornée* par un plafond. Cet exercice en calcule la
fonction pure, sans attendre réellement, et chacun de ses trois fragments porte une décision.

Le doublement d'abord : `1 << power` calcule deux puissance `power` par décalage de bits — la
forme entière exacte, préférable à `Math.Pow` qui transite par le flottant et rend un `double`
à reconvertir. Multiplié par la base de cent millisecondes, il déroule la suite cent,
deux cents, quatre cents... Le choix d'une croissance exponentielle plutôt que linéaire n'est
pas esthétique : après n échecs, le service en face est probablement en difficulté, et la
politique doit s'écarter *vite* — la charge de réessai décroît géométriquement, ce qui laisse
au service l'air nécessaire pour se rétablir.

Le plafond ensuite, et c'est le cœur de débogage de l'exercice. Sans lui, deux catastrophes
distinctes : d'abord des délais absurdes — la trente-et-unième tentative attendrait des
années —, ensuite et surtout le débordement — le décalage de bits au-delà de la taille du type
produit des valeurs fausses, potentiellement négatives, et un délai négatif passé à un
minuteur se comporte en « immédiat » : la politique de modération devient une mitrailleuse.
Le plafonnement `Math.Min(attempt, 5)` s'applique à *l'exposant*, avant le décalage — plafonner
le résultat après coup laisserait le débordement se produire dans le calcul. L'ordre
plafond-puis-calcul est la leçon : borner l'entrée du calcul dangereux, pas sa sortie.

La validation enfin : une tentative négative est une faute d'appel — le compteur de tentatives
appartient à l'appelant, et s'il est négatif, c'est son bug qu'il faut voir remonter. Zéro, en
revanche, est la première tentative légitime : cent millisecondes, le cas de l'exemple.

Les cas cachés longent la frontière du plafond : la tentative cinq — dernière croissance,
trois mille deux cents — et les tentatives au-delà, toutes égales au plafond ; plus le négatif
qui lève. La fonction étant pure et déterministe, elle se teste par table — c'est exactement
pourquoi l'exercice sépare le *calcul* du délai de son *application*, qui attendrait vraiment.

Il manque un raffinement que le contrat écarte sciemment : la variation aléatoire, qui
désynchronise les clients réessayant en chœur. Elle exigerait de l'aléa — non déterministe,
donc hors bac à sable — et c'est le premier ajout à faire en production. Savoir dire « voici ce
que ma fonction ne fait pas, et pourquoi il le faudra » : la transposition, ici, est cette
phrase-là.
