# Explication

Avancer exactement l'index de la valeur consommée et conserver les doublons.

La fusion tire son intérêt de l'ordre déjà présent : chaque valeur est examinée une fois, et le coût est la somme des deux longueurs. Concaténer puis trier donnerait le même résultat en payant un tri complet, c'est-à-dire en jetant l'information dont on disposait.

Deux fautes classiques cassent l'invariant. Avancer les deux index après une seule écriture saute une valeur ; oublier de vider l'entrée restante tronque le résultat. La comparaison inclusive au moment de départager rend la fusion stable, ce qui compte dès qu'on fusionne des objets sur une clé. L'espace correspond au résultat produit.
