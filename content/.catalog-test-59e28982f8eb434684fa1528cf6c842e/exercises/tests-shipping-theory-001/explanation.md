# Explication

Une grille de livraison à deux dimensions — un montant continu, un mode binaire — et une
question de méthode dans le titre : comment la couvrir par une *théorie*, un test paramétré qui
déroule des combinaisons ? L'exercice est autant un plan d'expérience qu'une règle à coder.

La règle d'abord, familière : l'express prime avec son tarif fixe, le mode normal offre la
gratuité à partir du seuil inclus. La structure garde-puis-décision transcrit la priorité, et
la question du recouvrement — express *et* gros panier — a sa réponse dans l'ordre des tests :
neuf quatre-vingt-dix, toujours.

Le plan d'expérience est le vrai sujet, et l'énoncé le pose : quelles combinaisons
*interagissent* réellement, lesquelles n'apprennent rien ? La grille naïve croise tout — chaque
montant intéressant fois chaque mode — et double le nombre de cas pour un gain nul sur la
moitié d'entre eux : en mode express, le montant *ne participe pas* à la décision, donc
multiplier les montants sous express vérifie dix fois la même constante. Les combinaisons
instructives se lisent dans la structure de la règle : en mode normal, le triplet de frontière
autour du seuil — cinquante exactement, juste en dessous, nettement au-dessus — parce que
c'est là que la décision vit ; en mode express, *un* montant de chaque côté du seuil suffit —
et le cas express-sous-le-seuil comme le cas express-au-dessus prouvent ensemble que le seuil
est bien inerte sous express, ce qui est précisément l'interaction à vérifier. Une théorie
bien construite énumère ces lignes-là et s'arrête : le critère n'est pas « toutes les
combinaisons » mais « chaque décision exercée, chaque non-interaction prouvée ».

Cette économie a un nom en conception de tests — l'analyse des interactions — et une
conséquence pratique : les lignes d'une théorie se lisent comme la spécification. Quiconque
ouvre le test paramétré doit pouvoir reconstituer la grille tarifaire depuis ses lignes ; si
des lignes redondantes noient les significatives, la théorie documente moins bien qu'elle ne
couvre.

Le total négatif lève avant toute grille — l'invariant monétaire habituel, qui vaut dans les
deux modes et mérite sa ligne de théorie aussi. Les tarifs sont des littéraux décimaux exacts,
sans calcul ni arrondi.

Le coût est constant. La transposition est la méthode de couverture des règles à plusieurs
paramètres : identifier quelles dimensions interagissent — en lisant la structure de la
règle —, poser les triplets de frontière sur les dimensions actives, une valeur témoin sur les
inertes, et une ligne qui prouve chaque inertie. C'est ce qui distingue une théorie de
quarante lignes illisibles d'une théorie de huit lignes qui est, à elle seule, le contrat.
