# Explication

Réduire un intervalle fermé et déplacer au-delà du milieu testé.

Deux détails décident de la correction. Le milieu se calcule à partir de la borne basse augmentée de la moitié de l'écart, et non de la somme des deux bornes : sur de grands indices, cette somme dépasse la plage entière et donne un milieu négatif. Et la borne se déplace au-delà du milieu déjà testé — sinon l'intervalle cesse de rétrécir et la boucle ne termine pas.

L'intervalle est fermé des deux côtés, donc la condition de poursuite est large : l'écrire stricte laisse l'élément final non testé. Le coût est logarithmique, ce qui suppose un tableau trié ; sur une entrée non triée, le résultat n'a aucun sens et rien ne le signale.
