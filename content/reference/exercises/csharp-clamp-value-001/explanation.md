# Explication

Refuser les bornes inversées puis traiter inférieur, supérieur et intervalle.

Des bornes inversées ne définissent aucun intervalle : c'est une faute d'appelant, pas un cas à absorber. Les échanger en silence masquerait le défaut chez celui qui l'a commis, et le résultat retourné n'aurait aucun sens vérifiable.

Une fois les bornes acceptées, trois partitions et deux frontières suffisent. Les bornes égales sont légitimes : elles décrivent un intervalle réduit à une valeur, et tout appel retourne alors cette valeur. Les comparaisons sont strictes, de sorte qu'une valeur exactement égale à une borne est retournée telle quelle sans passer par une branche d'écrêtage. La décision est en temps constant.
