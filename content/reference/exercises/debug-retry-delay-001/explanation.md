# Explication

Appliquer un report exponentiel déterministe et borné, sans attente réelle.

Le plafond de l'exposant est ce qui distingue un report utilisable d'une formule qui déborde : sans lui, quelques dizaines de tentatives suffisent à produire un délai absurde, puis une valeur négative. Le plafonner rend le délai maximal explicite et vérifiable.

Calculer le délai sans l'attendre est ce qui rend la règle testable : une fonction qui dort ne se teste qu'en dormant, et la suite devient lente et sensible à la charge de la machine. L'attente appartient à l'appelant, la règle au domaine — c'est la même séparation que pour l'horloge. La décision est en temps constant.
