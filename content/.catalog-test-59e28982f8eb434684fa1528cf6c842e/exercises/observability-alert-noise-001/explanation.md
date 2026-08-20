# Explication

La fatigue d'alerte n'est pas un problème de volume, c'est un problème de crédibilité. Chaque alerte
acquittée sans action apprend à l'équipe que les alertes ne veulent rien dire ; au bout de quelques
semaines, l'acquittement devient un réflexe, et la panne réelle — celle qui aurait justifié de se
lever — reçoit le même clic que les autres. La persistance est la réponse la plus simple à ce
problème, et l'exercice en fige la mécanique exacte, parce que les trois détails qui la composent
changent chacun le comportement du système d'astreinte.

**Pourquoi la persistance filtre bien.** Un pic isolé et une panne produisent le même échantillon au
premier instant ; ils divergent au deuxième. Le pic retombe — ramasse-miettes, rafale de trafic,
relance d'un service — quand la panne s'installe. Exiger plusieurs échantillons consécutifs, c'est
demander à la réalité de confirmer avant de réveiller quelqu'un. Le prix est explicite et se calcule :
une exigence de trois échantillons espacés d'une minute retarde toute alerte de deux minutes. Ce
retard n'est pas un défaut du réglage, c'est le taux de change entre réactivité et bruit — et le
choisir en connaissance de cause est un acte d'ingénierie, pas un curseur qu'on pousse.

**Pourquoi la remise à zéro est totale.** L'alternative — cumuler les dépassements à travers les
accalmies, par fenêtre glissante ou par compteur qui décroît — détecte d'autres choses, comme les
dégradations intermittentes. Mais pour une alerte de persistance, l'accalmie est une information : la
condition ne tient pas. Un compteur qui survivrait aux échantillons calmes finirait par déclencher sur
une série de pics disjoints, c'est-à-dire exactement sur le bruit que le réglage devait taire. La
sévérité de la remise à zéro est ce qui donne son sens au mot consécutif.

**Pourquoi l'indice rendu est celui qui complète la série.** L'alerte ne peut partir qu'au moment où
la condition devient vraie, et elle devient vraie à l'échantillon qui complète l'exigence — pas avant.
Rendre l'indice du début de la série serait réécrire l'histoire : à cet instant-là, rien ne
distinguait cette montée d'un pic sans lendemain. La distinction compte au-delà de l'esthétique : cet
indice horodate le déclenchement dans les revues d'incident, et un horodatage antidaté fausse le
calcul du délai de détection, la métrique que l'équipe cherche justement à améliorer.

**Le seuil exact compte, et la fenêtre vide n'alerte pas.** Au niveau ou au-dessus est un contrat :
l'exclure décalerait silencieusement tout le réglage d'un cran. Et une fenêtre sans mesure rend moins
un plutôt qu'un refus — l'absence de données est un état légitime du collecteur, à surveiller par un
autre signal que celui-ci.

La transposition : détection de saturation, seuils de température, échecs de connexion répétés —
partout où une mesure oscille, la persistance à remise à zéro stricte est le premier filtre à poser,
et son retard se budgète avant de la déployer.
