# Explication

Maintenir un préfixe trié contenant les plus petites valeurs.

L'invariant est l'image de l'algorithme : après l'étape numéro k, les k plus petites valeurs occupent définitivement le début du tableau. La recherche suivante peut donc se limiter au suffixe, et l'échange place directement la valeur à sa position finale.

Retenir l'indice du minimum et non sa valeur est ce qui rend l'échange possible — avec la seule valeur, on ne sait plus d'où elle vient. À la différence du tri par insertion, celui-ci effectue au plus un échange par position, ce qui le rend intéressant quand écrire coûte cher ; en revanche il compare toujours autant, même sur une entrée déjà triée.
