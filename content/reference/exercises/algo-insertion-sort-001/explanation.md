# Explication

Insérer chaque valeur dans un préfixe déjà trié.

L'invariant tient en une phrase : avant chaque étape, le préfixe qui précède l'élément courant est trié. L'étape consiste alors à ouvrir un trou à la bonne place, en décalant vers la droite tout ce qui est plus grand, puis à y déposer la valeur retenue.

Retenir la valeur avant de décaler est indispensable, puisque le premier décalage écrit sur sa case. Et la comparaison est stricte, ce qui rend le tri stable : deux valeurs égales conservent leur ordre d'origine, propriété qui compte dès qu'on trie des objets sur une clé partielle. Le coût est quadratique au pire et linéaire sur une entrée presque triée, ce qui explique son usage sur les petits segments des algorithmes hybrides.
