# Explication

Diviser par deux jusqu'à la borne et compter les réductions réellement faites.

La condition d'arrêt est la question : on s'arrête quand la valeur n'est plus divisible utilement, c'est-à-dire à un. Boucler tant que la valeur est non nulle ajoute une réduction qui n'a pas lieu d'être, et le résultat dépasse alors de un le nombre attendu. Zéro et un donnent tous deux zéro réduction.

La division entière est essentielle : en virgule flottante, la valeur décroîtrait indéfiniment sans jamais atteindre la borne. Ce compte est celui qui explique la complexité logarithmique de la recherche dichotomique — il donne une image concrète à une notation qu'on manipule souvent sans en avoir. La valeur négative est refusée plutôt qu'absorbée.
