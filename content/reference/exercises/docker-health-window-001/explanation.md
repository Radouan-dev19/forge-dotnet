# Explication

Une sonde de santé interroge le service à intervalle régulier et le déclare défaillant après un
nombre d'échecs consécutifs. La *fenêtre* — intervalle multiplié par essais — est le temps
maximal avant le verdict, et cette fonction vérifie qu'elle tient dans un budget. Le calcul est
une multiplication ; le dimensionnement qu'il encode est un vrai problème d'exploitation.

L'énoncé demande ce qu'une fenêtre trop courte fait d'un service qui démarre lentement : elle
le *tue en boucle*. Le service charge son cache, ouvre ses connexions — la sonde échoue ses
premiers essais, l'orchestrateur déclare l'échec et redémarre le conteneur, qui repart de zéro
et recommence. Le cycle porte un nom chez les exploitants — la boucle de redémarrage — et son
paradoxe est cruel : le service aurait été prêt à la sonde suivante, c'est le verdict trop
pressé qui fabrique la panne. La fenêtre inverse — trop longue — a le défaut symétrique : un
service réellement mort continue de recevoir du trafic pendant toute la fenêtre, et les
requêtes s'entassent sur un cadavre. Le budget de l'exercice représente cette exigence
extérieure — le temps de détection maximal que l'architecture tolère — et la fonction arbitre :
la sonde configurée tient-elle dedans ?

La mécanique suit le régime des prédicats de politique. Les trois valeurs se valident d'abord —
un intervalle, un nombre d'essais ou un budget non positifs ne décrivent aucune sonde, et le
verdict est un refus calme plutôt qu'une exception : la fonction juge des configurations, y
compris des configurations absurdes générées par un gabarit défaillant. La comparaison est
ensuite *inclusive* — la fenêtre exactement égale au budget passe, c'est le cas caché de
frontière — et le produit est `checked` : deux entiers plausibles pris séparément peuvent
déborder multipliés, et une fenêtre devenue négative par enroulement passerait *tous* les
budgets — le mensonge arithmétique exact que le mot clé transforme en exception.

Ce que la fonction ne modélise pas mérite une ligne : les sondes réelles ont aussi un délai de
démarrage — une période de grâce initiale — et un délai par essai ; la fenêtre complète les
additionne. L'exercice isole le produit central, et le laboratoire de conteneurisation du
parcours montre la configuration entière.

Les cas suivent l'énoncé : la fenêtre confortable, l'égalité exacte, le dépassement d'une
seconde, la valeur nulle.

Le coût est constant. La transposition est le réflexe de dimensionnement : pour chaque
mécanisme de détection — sonde, délai d'expiration, disjoncteur —, écrire la fenêtre totale en
secondes, la comparer au budget que le système d'au-dessus impose, et vérifier cette
inéquation par un test. Les boucles de redémarrage se conçoivent sur le papier, jamais à deux
heures du matin.
