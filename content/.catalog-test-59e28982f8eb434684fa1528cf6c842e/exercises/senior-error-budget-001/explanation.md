# Explication

Le budget d'erreur est l'outil qui transforme une promesse de fiabilite en decision operationnelle.
Un objectif de service, ou SLO, ne demande pas la perfection : viser 99,90 pour cent de succes,
c'est accepter d'avance que 0,10 pour cent des requetes echouent. Cette part toleree est le budget.
Tant qu'on ne l'a pas depense, on peut continuer a livrer et donc a prendre des risques ; des qu'on
le depasse, on gele les livraisons et on remet la fiabilite au premier plan. C'est ce que ce noyau
decide.

Le calcul se fait en deux temps qu'il ne faut pas confondre. Le premier convertit le SLO en un
nombre d'echecs autorises. La tolerance, exprimee en points de base, est le complement du SLO a
10000 : pour 9990, elle vaut 10. On l'applique au volume, `totalRequests` multiplie par cette
tolerance divise par 10000, en division entiere. Sur mille requetes a 99,90 pour cent, cela donne un
echec autorise ; sur dix mille, dix. Le second temps compare les echecs observes a ce budget. La
subtilite est le sens de la comparaison : le budget est epuise quand les echecs **depassent
strictement** la tolerance. Exactement au budget, on livre encore ; un de plus, on gele.

Les cas caches visent precisement cette borne. Dix mille requetes avec exactement dix echecs restent
sous le seuil et rendent `ship` ; onze echecs le franchissent et rendent `freeze`. Mille requetes
avec un seul echec rendent `ship`, parce qu'un echec autorise n'est pas un echec de trop ; deux
echecs rendent `freeze`. Un SLO de 10000 points de base, soit 100 pour cent, ne tolere aucun echec :
la moindre panne gele. Ces cas font la difference entre une comparaison large et une comparaison
stricte, l'erreur la plus frequente sur ce genre de calcul.

La validation des entrees ecarte les appels absurdes. Un total nul n'a pas de sens : il n'y a pas de
fenetre a evaluer. Des echecs negatifs ou superieurs au total, un SLO hors de l'intervalle des points
de base, signalent un bug de collecte plutot qu'une decision, et une exception d'argument vaut mieux
qu'un verdict fabrique sur des donnees fausses. Un detail compte aussi : le produit du volume par la
tolerance peut deborder un entier sur de gros volumes, il faut donc elargir le calcul intermediaire.

La transposition depasse le SLO d'un service unique. Un budget d'erreur distribue s'additionne le
long d'une chaine d'appels : chaque saut consomme sa part, et c'est la corrélation inter-service qui
permet d'attribuer un depassement au bon maillon. Savoir dire, chiffres en main, pourquoi on gele une
livraison est exactement ce qu'un entretien senior cherche a entendre : une decision de fiabilite
argumentee, pas une intuition.
