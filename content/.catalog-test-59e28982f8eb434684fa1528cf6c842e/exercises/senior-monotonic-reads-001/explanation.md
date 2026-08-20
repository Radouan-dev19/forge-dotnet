# Explication

La cohérence éventuelle se présente souvent comme un tout-ou-rien — « les répliques convergent,
acceptez le flou » — alors qu'elle se décompose en garanties précises, achetables une par une. Les
lectures monotones sont la plus petite d'entre elles et la plus rentable : elles ne promettent ni la
fraîcheur ni la convergence immédiate, seulement que **le temps ne recule pas pour un même client**.
C'est peu, et c'est exactement ce que les utilisateurs remarquent : le commentaire qui disparaît puis
réapparaît, le solde qui remonte, la commande « expédiée » qui redevient « en préparation ». Aucune
donnée n'est perdue dans ces scénarios ; la confiance, si.

**Pourquoi la détection compare des voisines et non un maximum.** La garantie porte sur la séquence
des lectures d'un client : chaque lecture doit être au moins aussi récente que la précédente. Comparer
au maximum global répondrait à une autre question — « suis-je revenu sous mon record » — qui confond
deux violations distinctes en une : un client qui lit 7, recule à 6, puis lit 8 et recule à 7 a subi
deux cassures de monotonie, mais une seule « chute sous le record ». Le journal d'enquête a besoin de
la définition exacte, parce que chaque cassure correspond à un saut de réplique qu'il faudra
corréler.

**Pourquoi l'égalité n'est pas un recul.** Deux lectures à la même version signifient que la donnée
n'a pas changé entre les deux — le cas le plus fréquent du monde. Le classer régression noierait le
rapport sous le normal, et pousserait à « corriger » un système qui fonctionne. La frontière stricte
est ici tout le contenu de la définition : monotone signifie jamais strictement décroissant, pas
strictement croissant.

**Pourquoi l'indice fautif est celui qui recule.** L'enquête part de la lecture servie en retard :
son horodatage, sa réplique, le décalage de réplication à cet instant. Pointer la lecture haute —
celle d'avant — enverrait l'astreinte examiner la réplique qui n'a rien fait de mal. Et la première
cassure suffit : le rapport déclenche une enquête, il ne dresse pas l'inventaire — les cassures
suivantes ont presque toujours la même cause, et l'inventaire se fera outillé, pas à l'œil.

**Les deux parades côté système méritent d'être nommées.** La première colle chaque client à une
réplique — la session affinitaire : simple, mais elle meurt avec la réplique. La seconde fait porter
au client la version de sa dernière lecture, que le serveur honore en choisissant une réplique au
moins aussi avancée — plus robuste, au prix d'un jeton qui circule. Les deux transforment une
promesse implicite en contrat vérifiable, et le détecteur de cet exercice est précisément l'outil qui
mesure si le contrat tient.

En entretien, le terme attendu est monotonic reads, au sein de la famille des garanties de session —
et la bonne réponse commence par distinguer ce que la garantie promet de ce qu'elle ne promet pas.
