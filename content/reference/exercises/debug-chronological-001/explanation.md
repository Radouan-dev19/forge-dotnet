# Explication

Autoriser les égalités et détecter la première inversion sans trier l'entrée.

Trier détruit exactement l'information recherchée : après un tri, toute suite est croissante, et la question n'a plus de réponse. La vérification se fait donc sur place, par comparaison de chaque élément avec son prédécesseur immédiat.

Les égalités sont acceptées : deux événements peuvent porter le même horodatage, ce qui est fréquent quand la résolution est la seconde. Refuser l'égalité produirait des faux positifs sur des journaux parfaitement normaux. La suite vide et la suite d'un seul élément sont chronologiques sans qu'aucune comparaison n'ait lieu. Le parcours est linéaire et s'arrête au premier recul.
