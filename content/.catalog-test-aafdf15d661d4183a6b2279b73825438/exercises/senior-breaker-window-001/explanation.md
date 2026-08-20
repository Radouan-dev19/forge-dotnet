# Explication

Le disjoncteur est le motif de résilience le plus cité en entretien et le plus mal réglé en
production, parce que sa difficulté n'est pas l'automate — trois états, quatre transitions — mais les
**conditions de mesure** qui autorisent chaque transition. Cet exercice isole précisément ces
conditions, et chacune corrige un accident type.

**La garde de volume avant le taux, toujours.** Le taux d'échec est une fraction, et les fractions
mentent sur les petits dénominateurs : deux échecs sur deux appels font cent pour cent, comme deux
échecs sur deux mille font un dixième de pour cent. Un disjoncteur qui juge le taux sans juger le
volume coupe des services sains à chaque creux de trafic — la nuit, un unique appel malchanceux
ouvre le circuit, et l'ouverture se voit au réveil sous forme d'un service « en panne » qui ne
l'était pas. Le volume minimal n'est pas une optimisation, c'est la condition de signification de la
mesure ; d'où son rang dans la cascade : l'insuffisance de données se déclare avant même de calculer
le taux.

**Le taux en produits croisés, pas en pourcentage.** La comparaison `failures × 100 > max-rate ×
calls`, en entiers larges, pose la même question que le pourcentage sans fabriquer de flottant : pas
d'arrondi qui ouvre le circuit à quarante-neuf virgule neuf-neuf, pas de débordement sur une fenêtre
de deux milliards d'appels. Et l'inégalité est stricte : un taux exactement au maximum toléré est
toléré. Ce choix se discute — certains outils ouvrent à l'égalité — mais il doit être écrit et testé,
parce que c'est sur cette frontière exacte que deux implémentations « équivalentes » divergent en
production.

**L'échec de sonde prime sur le compte de sondes.** L'état demi-ouvert existe pour poser une
question au service : es-tu remis ? Une seule sonde en échec répond non, et ce non ne se négocie pas
contre un compte — fermer parce que « quatre sondes sur cinq sont passées » réinjecte tout le trafic
sur un service qui vient d'échouer, et le cycle ouverture-fermeture qui s'ensuit est pire que
l'ouverture franche : il métronome la charge. La cascade du demi-ouvert vérifie donc l'échec d'abord,
le compte ensuite.

**Chaque état exige exactement ses mesures.** Un relevé qui mélange les mesures de deux états — un
refroidissement fourni avec une fenêtre fermée — ne décrit aucun instant réel du disjoncteur : c'est
un collecteur qui a fusionné deux instantanés, et décider dessus produirait une transition plausible
sur un état imaginaire. Le refus strict est la version mesurable d'un principe qui traverse la piste
senior : on ne décide que sur un relevé cohérent.

En entretien comme en production, ce motif se nomme circuit breaker, et les bibliothèques de
résilience le fournissent réglable — la valeur du candidat n'est pas de le recoder, mais de savoir
régler ces quatre conditions et d'expliquer ce que chacune évite.
