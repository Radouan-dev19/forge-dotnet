# Explication

Une remise paramétrée par son taux : après l'addition de montants, c'est la deuxième marche du
domaine monétaire, et elle introduit ce que la première n'avait pas — un domaine d'entrée à
défendre et un point d'arrondi à choisir.

Le domaine du taux d'abord, parce qu'il est la vraie nouveauté. Un taux est ici une *fraction* :
zéro ne remise rien, un remise tout, et l'intervalle fermé entre les deux couvre tous les cas
légitimes. Les deux bornes sont incluses et *signifiantes* — le taux zéro rend le total inchangé
(arrondi au centime près), le taux un rend exactement zéro — et les cas cachés les éprouvent
toutes deux, car ce sont des valeurs qu'un système réel produit : promotion inactive, article
offert. Au-delà de un, la « remise » deviendrait un net négatif : le client serait payé pour
acheter. Plutôt que de plafonner en silence, la solution refuse — un taux de un et demi est une
erreur de l'appelant, probablement un pourcentage (quinze) passé là où une fraction était
attendue, et c'est *ce* bug-là que l'exception fait remonter. La confusion
pourcentage-contre-fraction est l'erreur d'intégration la plus fréquente du domaine, et la
validation de borne haute est le filet qui l'attrape à la frontière.

Le calcul reprend la forme directe — total fois un moins le taux — avec l'unique arrondi final
au centime, règle commerciale nommée. Les raisons sont celles du domaine entier : la pleine
précision pendant le calcul, la sortie arrondie une fois au point métier, et
`AwayFromZero` plutôt que le défaut bancaire pour que le demi-centime monte comme le client s'y
attend. Ce qui change par rapport à la remise VIP voisine, c'est la *provenance* du taux : là il
était choisi par une branche, ici il arrive en paramètre — la politique s'est déplacée chez
l'appelant, et la fonction est devenue le mécanisme pur, réutilisable par toutes les politiques.
Cette gradation — politique câblée, puis politique paramétrée — est un motif de conception à
reconnaître : quand les variantes se multiplient, on remonte la décision et on garde le calcul.

Le total négatif reste refusé, zéro reste licite — remiser zéro donne zéro, sans cas spécial.
L'exception ne nomme pas de paramètre, deux pouvant fauter à la fois ; on peut discuter ce
choix, une version qui valide séparément donnerait un diagnostic plus fin au prix de deux
gardes.

Le coût est constant. La transposition : taux de taxe, de commission, de pénalité — chaque
fraction paramétrée mérite son intervalle écrit, ses deux bornes testées, et le filet contre le
pourcentage égaré. C'est trois lignes de garde pour des années de factures justes.
