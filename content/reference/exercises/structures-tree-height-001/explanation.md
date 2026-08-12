# Explication

Suivre les parents jusqu'à la racine et refuser un cycle par une garde bornée.

La hauteur est le maximum des profondeurs, et rien ne dit quel nœud la porte : il faut donc remonter depuis chacun. C'est ce qui explique le coût quadratique au pire, celui d'une chaîne où chaque remontée traverse presque tout l'arbre. Une seconde passe mémorisant les profondeurs déjà calculées ferait mieux, au prix d'un tableau supplémentaire.

La garde est la même que pour le comptage d'ancêtres : au-delà d'autant de sauts qu'il y a de nœuds, un cycle est certain. Une racine seule a une profondeur de un, puisqu'on compte les niveaux et non les arêtes. L'espace se limite à quelques compteurs.
