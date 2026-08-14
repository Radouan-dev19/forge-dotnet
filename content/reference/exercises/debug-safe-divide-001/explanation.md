# Explication

Deux lignes utiles : si le diviseur est nul, zéro ; sinon, la division entière. La brièveté est
trompeuse — cet exercice porte l'une des décisions les plus discutées du code défensif, et la
discuter honnêtement vaut mieux que la réciter.

Que faire d'un diviseur nul ? La division machine lève `DivideByZeroException`, et la fonction
la remplace par une valeur de repli : zéro. C'est un choix de *robustesse locale* — un tableau
de bord qui divise un total par un compte parfois nul préfère afficher zéro que s'effondrer —
et il a un coût qu'il faut nommer : le zéro de repli est indistinguable d'un vrai quotient nul.
Un taux calculé à zéro parce qu'il n'y avait rien à mesurer et un taux réellement nul
racontent deux histoires différentes, et cette fonction les fond en une seule. L'alternative —
laisser lever, ou retourner un type optionnel qui force l'appelant à décider — préserve
l'information au prix de la simplicité. Le contrat de l'exercice choisit le repli, et le nom
`SafeDivide` l'annonce ; la leçon est que ce choix doit toujours être *un* choix, nommé et
testé, jamais l'effet d'un rattrapage d'exception paresseux autour d'un calcul entier.

La seconde clause du contrat est plus discrète : *conserver la division entière annoncée*. Huit
divisé par deux fait quatre, mais sept divisé par deux fait trois — troncature vers zéro, le
comportement natif des entiers — et il faut résister à la tentation d'« améliorer » en
arrondissant ou en passant au décimal. La division entière est une opération à part entière,
avec ses usages — pagination, répartition par lots, indices — et ses règles : la troncature va
vers zéro, donc moins sept divisé par deux fait moins trois, pas moins quatre. Le cas caché aux
opérandes négatifs fige ce comportement, que les habitués d'autres langages — où la division
entière plancher vers le bas — se tromperaient à prédire.

Les cas cachés couvrent ainsi les trois axes : le diviseur nul qui rend zéro au lieu de lever,
la troncature — dividende non multiple —, et les signes croisés. Le dividende nul, lui, rend
zéro par le calcul ordinaire : deux chemins vers zéro, et c'est précisément l'ambiguïté
assumée du contrat.

Le coût est une comparaison et une division. La transposition est le questionnaire du repli :
chaque fois qu'un calcul peut être indéfini — division, logarithme, accès par clé —, trois
réponses possibles — exception, valeur de repli, type optionnel — et trois questions pour
choisir : qui appelle, que fait-il de l'ambiguïté, et le repli peut-il être confondu avec un
résultat légitime ? La réponse écrite dans le nom de la fonction, comme ici, est la moitié de
la documentation.
