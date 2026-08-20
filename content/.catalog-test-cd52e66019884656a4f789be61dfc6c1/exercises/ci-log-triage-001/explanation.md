# Explication

Le journal d'un pipeline rouge contient presque toujours plus de rouge que de problèmes. Un échec en
début de chaîne annule les travaux qui en dépendaient, une relance transforme un échec en réussite
sans effacer la trace, et un travail conditionnel se saute lui-même. La personne d'astreinte qui lit
ce mur au premier degré ouvre trois pistes pour un seul défaut — et l'exercice encode le raisonnement
qui n'en ouvre qu'une.

**La consolidation d'abord : le verdict d'un travail est sa dernière entrée.** Les relances sont un
fait de la vie des chaînes d'intégration — réseau, dépôts de paquets, machines partagées — et le
journal les consigne toutes. Juger un travail sur sa première entrée reviendrait à compter comme
bloquant un incident déjà résolu par la relance ; le juger sur « au moins un échec quelque part »
noierait le diagnostic sous les faux positifs des travaux instables. La dernière entrée est la seule
qui décrive l'état final du pipeline, celui qui a déterminé sa couleur. Le corollaire mérite d'être
dit : un journal plein d'échecs effacés qui finit vert au sens des verdicts finaux rend « aucun
bloquant » — c'est exact, et c'est un autre problème, celui de l'instabilité, qui se traite avec
d'autres outils.

**La hiérarchie ensuite : l'échec avant l'annulation, quelle que soit la position.** Une annulation
est ambiguë par nature : elle peut être la conséquence mécanique d'un échec en amont — l'orchestrateur
arrête ce qui n'a plus de sens — ou la cause elle-même, quand un humain ou un délai a interrompu la
campagne. Le journal ne code pas cette différence, mais la logique la reconstitue : s'il existe un
échec final quelque part, c'est lui la cause première, et les annulations sont ses victimes ; s'il
n'en existe aucun, la première annulation devient la seule explication du rouge. Trier par position
d'abord — « le premier rouge est le coupable » — inverserait ce raisonnement dès qu'une annulation
précède l'échec dans le journal, ce qui arrive dès que l'orchestrateur annule des travaux en parallèle
du travail encore en train d'échouer.

**Les sautés ne concourent jamais.** Un travail sauté n'a pas échoué : sa condition d'exécution a
répondu non. Le compter comme bloquant transformerait chaque chemin conditionnel en fausse alerte.

**Pourquoi rendre le verdict avec le nom.** Le nom seul obligerait le lecteur à retourner au journal
pour savoir s'il cherche un échec ou une interruption ; le verdict seul ne dirait pas où aller. La
paire nom-verdict est la plus petite réponse qui déclenche la bonne action — ouvrir les journaux du
bon travail avec la bonne question en tête.

La transposition va au-delà des pipelines : files de traitement, chaînes de déploiement,
orchestrations de tâches — partout où des travaux dépendent les uns des autres, le tri cause-victime
et la consolidation des relances précèdent tout diagnostic honnête.
