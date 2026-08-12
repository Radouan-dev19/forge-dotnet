# Explication

Refuser des bornes incohérentes puis comparer exactement les deux frontières.

Une frontière est une valeur exacte, pas un voisinage : la comparaison est une égalité, et les deux extrémités comptent. N'en tester qu'une est l'oubli le plus fréquent, et il laisse passer la moitié des erreurs de un.

Des bornes inversées ne définissent aucun intervalle : les refuser vaut mieux que de les interpréter comme un intervalle vide, ce qui masquerait la faute chez l'appelant. Des bornes égales, en revanche, sont légitimes — elles décrivent un intervalle réduit à une valeur, qui est alors ses deux frontières à la fois. La décision est en temps constant.
