# Explication

Traiter le diviseur nul et conserver la division entière annoncée.

Le repli est une décision, pas une évidence : retourner une valeur convenue rend la fonction utilisable sans garde préalable, au prix d'une information perdue — l'appelant ne distingue plus un quotient nul d'une division impossible. Lever aurait été défendable ; ce qui ne l'est pas, c'est de ne pas choisir.

La division reste entière, comme annoncé : passer en flottant changerait le type du résultat et l'arrondi. Un cas mérite d'être connu au-delà de l'exercice : diviser la plus petite valeur signée par moins un déborde, parce que le quotient n'est pas représentable. La décision est en temps constant.
