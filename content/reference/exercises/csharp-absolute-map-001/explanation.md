# Explication

Projeter vers un nouveau tableau et ne pas muter la source.

Le résultat a exactement la longueur de l'entrée : c'est une projection, pas un filtre. Allouer un nouveau tableau rend le contrat d'immutabilité observable par l'appelant ; retourner la référence reçue lorsqu'aucune valeur n'est négative le romprait silencieusement, et le défaut n'apparaîtrait que le jour où quelqu'un modifie le résultat.

Un cas mérite d'être connu : la plus petite valeur d'un entier signé n'a pas d'opposé représentable, et en demander la valeur absolue lève une exception de dépassement. Le parcours est linéaire et l'espace correspond au tableau produit.
