# Explication

Comparer deux numéros de version ressemble à comparer deux chaînes. C'est l'erreur qui déploie une
version périmée en production, et elle survit longtemps parce qu'elle ne se manifeste qu'au passage
de la dizaine.

**Segment par segment, comme des nombres.** Dans l'ordre du dictionnaire, `1.2.10` précède `1.2.9` :
le caractère `1` vient avant le caractère `9`, et la comparaison s'arrête là. Une chaîne d'outils qui
trie ainsi désigne la neuvième correction comme la plus récente. Le défaut ne se voit pas pendant les
neuf premières versions — c'est ce qui le rend redoutable. Il apparaît le jour de la dixième, dans un
pipeline que plus personne ne relit, et il faut du temps pour soupçonner le tri.

**L'arrêt au premier écart n'est pas une optimisation.** C'est la sémantique du versionnage. Un
majeur supérieur signifie une rupture de compatibilité ; il l'emporte quelles que soient les valeurs
qui suivent. `2.0.0` est postérieure à `1.99.99`, et un algorithme qui continuerait à comparer après
avoir vu `2 > 1` risquerait de laisser le mineur `0 < 99` renverser une décision déjà prise. Écrire
la boucle avec un retour immédiat à l'écart rend cette priorité structurelle plutôt qu'accidentelle.

**Le refus d'un format variable est un choix, et il se défend.** On pourrait accepter deux segments
en complétant par un zéro implicite, ou quatre en ignorant le dernier. Chaque tolérance introduit une
convention silencieuse que l'appelant ne connaît pas : `1.2` vaut-il `1.2.0` ou signale-t-il une
version dont le correctif n'est pas encore décidé ? Refuser oblige à trancher là où l'information
existe, et évite qu'une comparaison rende un résultat plausible mais faux.

**La conversion refuse aussi le signe.** Un segment négatif ne correspond à aucune version publiée ;
l'accepter reviendrait à comparer une valeur qui n'aurait pas dû franchir la lecture. Le style de
conversion choisi interdit le signe explicitement, ce qui vaut mieux qu'un test ajouté après coup et
qu'on oublierait sur un des trois rangs.

**Les zéros de tête sont un cas instructif** : `1.02.0` et `1.2.0` désignent la même version, parce
que le segment est lu comme un nombre. Une implémentation par comparaison de chaînes les
distinguerait, et croirait à une différence là où il n'y en a pas. Le cas est rare en pratique, mais
il révèle immédiatement si la conversion a bien eu lieu.

**Ce que le modèle laisse de côté**, et qu'il faut savoir citer : le versionnage sémantique complet
autorise des suffixes de pré-publication — `1.0.0-rc.1` — qui se comparent selon des règles propres,
une pré-publication étant antérieure à la version finale du même numéro. Les intégrer changerait la
lecture, pas la structure de la comparaison.

Le coût est constant : trois segments, trois comparaisons au plus.
