# Explication

Une absence attendue reçoit un repli explicite, distinct d'une erreur métier.

Toute absence n'est pas une faute. Ici le contrat annonce qu'une valeur manquante est prévue et reçoit un repli : lever imposerait à chaque appelant un traitement d'exception pour un cas normal. C'est la distinction entre une valeur optionnelle et une violation d'invariant.

Trois entrées se ramènent au même repli : absente, vide, composée de blancs. Le repli lui-même mérite une seconde d'attention — il doit être reconnaissable comme tel et ne jamais pouvoir être confondu avec une donnée réelle, sans quoi il devient impossible de savoir en aval si l'information manquait. Le coût est linéaire dans la longueur de la chaîne.
