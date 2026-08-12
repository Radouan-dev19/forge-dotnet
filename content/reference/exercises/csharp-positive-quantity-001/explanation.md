# Explication

Lever une exception de contrat seulement pour une valeur négative.

La frontière est zéro, et zéro est accepté : une quantité nulle est légitime dans la plupart des domaines, une ligne à zéro article existe. Refuser zéro est le contresens le plus fréquent sur ce type de garde, et il ne se voit qu'à l'usage.

Corriger silencieusement — en prenant la valeur absolue, ou en ramenant à zéro — est plus grave que refuser : le défaut reste chez l'appelant, il produit un résultat plausible, et la cause devient introuvable. Nommer le paramètre dans l'exception est ce qui rend le diagnostic immédiat. La décision est en temps constant.
