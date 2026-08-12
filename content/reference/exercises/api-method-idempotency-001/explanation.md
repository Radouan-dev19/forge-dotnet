# Explication

Comparer une méthode normalisée à la liste explicite des méthodes idempotentes.

Deux propriétés voisines se confondent facilement. Une méthode sûre ne modifie pas l'état ; une méthode idempotente peut le modifier, à condition que le répéter ne change rien de plus. Le remplacement complet d'une ressource et sa suppression sont idempotents sans être sûrs : rejouer la suppression laisse le même état final, même si la seconde réponse diffère.

La création est l'exception qui donne son sens à la distinction : la rejouer crée une seconde ressource. C'est aussi pourquoi un client peut réessayer sans risque une requête idempotente après un délai, et pas une création. La liste est fermée et la comparaison normalisée : le coût est linéaire dans la longueur du nom.
