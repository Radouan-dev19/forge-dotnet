# Explication

Un jalon de livraison est révisable quand trois preuves existent : les tests passent, la
sécurité a été revue, le retour arrière est documenté. Encore une conjonction de trois — la
forme est désormais familière — et la valeur est dans le choix de *ces* trois preuves, qui
couvrent trois familles de risque orthogonales.

Les tests verts couvrent le risque *fonctionnel* : le système fait ce qu'il promet. C'est la
preuve la plus automatisée et la plus visible — et l'énoncé demande précisément ce qu'elle
laisse passer sans sa voisine : tout ce qui fonctionne *trop bien*. Une route de diagnostic
exposée sans contrôle d'identité passe les tests fonctionnels avec brio — elle fonctionne. Un
journal qui écrit les jetons en clair fonctionne. Une dépendance vulnérable fonctionne. La
revue de sécurité couvre ce risque-là — non pas « ça marche ? » mais « qu'est-ce que ça
expose ? » — et aucun volume de tests fonctionnels ne la remplace, parce que les deux
questions regardent dans des directions différentes.

Le retour arrière documenté couvre le troisième risque, celui qu'on oublie parce qu'il ne se
matérialise qu'en cas de malheur : *et si la livraison tourne mal ?* La procédure écrite —
comment revenir, en combien de temps, avec quelles données perdues ou migrées — se rédige à
froid, avant la livraison, quand tout le monde réfléchit bien. La version improvisée à chaud,
pendant l'incident que la livraison a causé, est systématiquement pire — et parfois
impossible : certaines migrations ne se défont pas, et *le savoir avant* change la stratégie
de livraison elle-même. Un retour arrière documenté est le test de réversibilité de la
livraison ; son absence signifie qu'on ne sait pas si on peut reculer.

La conjonction stricte, une fois encore, refuse les jalons à deux preuves — le presque-prêt
qui a l'air prêt. Les cas couvrent le complet et chaque preuve retirée isolément, domaine
booléen assumé.

Cette grille est celle du projet final du parcours, dont les jalons portent exactement ces
preuves — la fonction est le noyau décidable d'une règle que la grille d'évaluation applique
à des livrables réels.

Le coût est constant. La transposition est la définition de « terminé » d'une équipe : chaque
jalon important mérite sa liste de preuves — fonctionnelle, sécurité, réversibilité — et la
liste se vérifie mécaniquement quand c'est possible, à la revue sinon. Le mot important est
*preuves* : « on a testé » n'en est pas une, le rapport de tests en est une ; « on peut
revenir en arrière » n'en est pas une, la procédure datée en est une. Les jalons se franchissent
sur pièces.
