# Explication

Dédupliquer avant de trier et matérialiser exactement une fois.

L'ordre des deux opérations change le coût sans changer le résultat : dédupliquer d'abord réduit ce que le tri doit ordonner, et sur une entrée à forte redondance la différence est nette. C'est l'exemple le plus simple d'une décision de composition qui ne se lit pas dans le résultat.

La matérialisation en fin de chaîne est le second point : une séquence paresseuse retournée telle quelle serait réévaluée à chaque parcours par l'appelant, ce qui multiplie silencieusement le coût annoncé. Le tri domine le temps, et l'espace correspond au résultat produit.
