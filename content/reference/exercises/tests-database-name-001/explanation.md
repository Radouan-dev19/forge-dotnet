# Explication

Vérifier qu'un nom de base de données désigne bien une base de test isolée : le prédicat tient
en deux conditions, et chacune protège contre une catastrophe distincte — c'est un exercice de
convention d'infrastructure déguisé en comparaison de chaînes.

Le préfixe réservé d'abord, et l'énoncé demande ce qu'il rend sûr : le *nettoyage*. Les tests
d'intégration créent des bases jetables, et quelque chose doit les détruire — après la suite,
ou périodiquement quand des exécutions interrompues en laissent traîner. Ce quelque chose est
un script qui supprime *par motif de nom* : la seule garantie qu'il ne supprimera jamais une
base réelle est que les bases de test vivent dans un espace de noms que rien d'autre n'a le
droit d'occuper. Le préfixe `forge-test-` est ce territoire : la convention coûte zéro et
transforme « supprimer ce qui ressemble à du test » — une roulette — en « supprimer ce qui est
dans le territoire réservé » — une opération sûre. La comparaison est ordinale, comme tout
identifiant technique, et sensible à la casse : un préfixe approximatif n'est pas le
territoire.

La longueur minimale ensuite, moins évidente : vingt caractères au total, soit au moins neuf
après le préfixe. Ce plancher garantit un *suffixe d'unicité* substantiel — horodatage,
fragment aléatoire — et c'est l'isolation entre exécutions *simultanées* : deux suites qui
tournent en parallèle sur la même instance ne doivent jamais partager une base, sinon leurs
données se polluent mutuellement et les échecs deviennent aléatoires — les pires à
diagnostiquer. Un nom court comme `forge-test-1` a probablement été écrit à la main, et les
noms écrits à la main entrent en collision. Le prédicat ne vérifie pas *la qualité* du
suffixe — il ne peut pas — mais sa présence en volume, ce qui écarte déjà la paresse. Le cas
caché au suffixe trop court d'un caractère verrouille la borne exacte.

Le nom vide ou blanc répond faux — verdict calme, cohérent avec un prédicat de filtrage qu'un
script de nettoyage appliquera à des listes entières de noms.

Les cas suivent l'énoncé : le conforme, le préfixe absent — y compris le presque-bon —, le
suffixe d'un caractère trop court, et le vide.

Le coût est constant. La transposition est le motif du *territoire nommé* : files de test,
conteneurs éphémères, répertoires temporaires, sujets de messagerie — tout ce que
l'automatisation crée et détruit en masse mérite un préfixe réservé, un suffixe d'unicité, et
un prédicat comme celui-ci, testé, entre le script de nettoyage et le désastre. La règle en
une phrase : on ne supprime en masse que dans un espace de noms qu'on possède.
