# Explication

La borne est strictement dépassée et la valeur absolue est calculée dans un type plus large.

La plus petite valeur d'un entier signé n'a pas d'opposé représentable : en demander la valeur absolue dans le même type lève une exception de dépassement. Passer par un type plus large avant le calcul supprime le cas particulier, sans branche supplémentaire — c'est le genre de détail qu'un débogage découvre en production et qu'une lecture attentive du domaine évite.

Le seuil est strictement dépassé, donc une valeur exactement égale n'est pas une anomalie. C'est la seule frontière du problème, et elle se teste dans les deux sens. Le parcours est linéaire et seul le compteur occupe l'espace.
