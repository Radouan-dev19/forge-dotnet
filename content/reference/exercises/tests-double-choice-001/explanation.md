# Explication

Choisir le constat pour l'interaction, l'implémentation simplifiée pour le comportement, la réponse fixe sinon.

L'ordre traduit une règle simple : si un résultat peut être vérifié, il ne faut pas vérifier l'appel. Le besoin d'interaction ne se pose donc que lorsque l'effet de bord *est* le résultat — un envoi, une publication, une écriture d'audit — et il prime alors sur toute autre considération.

Le second critère distingue une réponse d'un comportement. Écrire puis relire exige une implémentation simplifiée qui garde un état ; obtenir une valeur pour que le code sous test continue n'exige qu'une réponse fixe. Choisir trop riche coûte en maintenance ; choisir trop pauvre rend le test impossible à écrire. La décision est en temps constant.
