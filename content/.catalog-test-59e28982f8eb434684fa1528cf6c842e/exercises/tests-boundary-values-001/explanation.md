# Explication

Détecter si une valeur est une frontière d'intervalle : la fonction est un double test
d'égalité, et son sujet réel est la *méthode des valeurs limites* — la technique de test la
plus rentable qui existe, que cet exercice fait pratiquer en la codant.

Le constat fondateur de la méthode : les défauts ne se répartissent pas uniformément dans un
domaine d'entrée — ils s'agglutinent aux frontières, parce que c'est là que les comparaisons se
décident, et qu'un seul caractère d'écart entre `<` et `<=` déplace une frontière d'exactement
une unité. L'énoncé demande de nommer l'erreur que le triplet autour d'un seuil attrape et que
la valeur du seuil seule laisserait passer : c'est le décalage. Un test posé uniquement *sur* le
seuil passe aussi bien avec la borne incluse qu'avec une borne décalée d'un cran du bon côté —
il valide deux implémentations différentes. Le triplet — la frontière, sa voisine intérieure,
sa voisine extérieure — épingle la position *et* l'inclusivité : trois valeurs par frontière,
et la classe d'erreurs entière disparaît. Cette discipline du triplet est ce que les exercices
du catalogue appliquent partout ; ici, elle devient l'objet d'étude.

La fonction elle-même distingue frontière et *intérieur* : une valeur entre les bornes est dans
l'intervalle mais n'est pas une frontière — le prédicat est l'égalité exacte avec l'une des
extrémités, pas l'appartenance. Cette distinction est celle du vocabulaire de test : les
valeurs intérieures peuplent les cas nominaux, les frontières peuplent les cas limites, et un
plan de test les nomme séparément.

Le cas dégénéré mérite le détour : quand les deux bornes coïncident, l'intervalle est un point,
et ce point est frontière des deux côtés — le prédicat répond vrai par l'une ou l'autre
égalité, sans code spécial. Les bornes inversées, elles, ne décrivent rien et lèvent — la
cohérence de l'intervalle se valide avant de raisonner dessus, comme dans le bornage de
valeurs voisin.

Les cas cachés suivent la méthode qu'ils enseignent : chaque extrémité répond vrai, la voisine
intérieure et l'extérieure répondent faux, l'intervalle-point répond vrai, l'inversé lève.

Le coût est constant. La transposition est la méthode elle-même, à dérouler désormais par
réflexe : pour chaque frontière d'un contrat — seuil de remise, plafond de page, date
d'expiration —, trois cas de test, position plus inclusivité. Et en entretien, savoir
*expliquer pourquoi* trois et pas un — le décalage indétectable — vaut plus que la récitation
de la technique.
