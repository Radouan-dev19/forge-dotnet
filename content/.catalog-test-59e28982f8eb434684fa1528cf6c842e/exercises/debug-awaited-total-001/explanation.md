# Explication

La somme est banale ; le nom des choses ne l'est pas. `AwaitedTotal`, `completedResults` : cet
exercice isole *l'étape d'agrégation* d'un motif asynchrone — lancer plusieurs travaux, attendre
tous les résultats, les combiner — et le fait dans le seul cadre que le bac à sable permet, une
fonction pure sur les résultats déjà obtenus. Comprendre ce découpage est la moitié de la
leçon.

Le motif complet, dans du code réel, s'écrit : démarrer toutes les tâches *d'abord* — pour
qu'elles courent en parallèle —, les attendre *ensemble* ensuite, puis agréger. Les deux
premières étapes portent leurs propres pièges — démarrer-puis-attendre dans la même boucle
sérialise tout, oublier une tâche perd son résultat en silence — et l'énoncé y fait allusion :
« aucun travail lancé ne doit être oublié ». L'agrégation, elle, doit être *déterministe et
totale* : chaque résultat compte exactement une fois, l'ordre d'arrivée n'influence pas le
total — l'addition est associative et commutative, c'est précisément pourquoi la somme est
l'agrégat le plus sûr à paralléliser — et aucun résultat n'est filtré au passage. La boucle de
la solution transcrit cette totalité : pas de condition, pas de saut, un cumul.

Les deux gardes du cumul sont les habituées du catalogue, replacées dans leur contexte
asynchrone où elles mordent plus fort. Le `null` signale une faute d'appel — un lot de
résultats absent n'est pas un lot vide, et dans un pipeline de tâches, cette confusion masque
typiquement une étape d'attente sautée. Le `checked` transforme le débordement en exception
franche : un total de travaux qui s'enroule en négatif, publié dans un rapport, est le genre de
mensonge qui survit des semaines — les résultats individuels sont plausibles, seul leur cumul
ment. Le tableau vide rend zéro : aucun travail, total nul, la convention du neutre — et dans
le motif complet, c'est le cas « aucune tâche lancée », parfaitement légitime.

Les cas cachés jouent les variations attendues : négatifs mêlés — un résultat peut être un
écart, pas seulement un compte —, lot vide, lot d'un seul, et une disposition qui réfute le
total figé.

Le coût est linéaire, sans allocation. La transposition est le motif entier, à savoir dérouler
en entretien : lancer sans attendre, attendre en bloc, agréger totalement — et ses trois
défauts symétriques : sérialiser par impatience, perdre par oubli, corrompre par débordement.
L'exercice ne fait travailler que le troisième tiers ; les leçons du parcours asynchrone
couvrent les deux autres, et le nom de la fonction est là pour qu'on les relie.
