# Explication

Projeter un nom vers un libellé de DTO : la fonction tient en une expression, et l'exercice
existe pour la question que son titre pose en creux — pourquoi une couche de projection
existe-t-elle, plutôt que d'exposer l'entité de persistance telle quelle ?

L'énoncé demande de nommer ce qu'exposer l'entité entière publierait, et la liste est
édifiante : les colonnes techniques — identifiants internes, horodatages d'audit, versions de
concurrence —, les champs sensibles qui n'ont rien à faire dans une réponse d'API, et surtout
la *forme* du schéma de base, qui devient alors un contrat public par accident. Chaque
consommateur qui s'y attache transforme la moindre migration de colonne en rupture d'API. Le
DTO est la digue : une forme *choisie*, stable par décision et non par inertie, où chaque champ
présent l'est parce qu'on l'a voulu. La projection — entité vers DTO — est l'endroit où cette
volonté s'exerce, champ par champ, et cet exercice en isole un champ.

Le champ lui-même applique le traitement des textes de frontière, dans sa version sortante :
rogner les bords — la donnée stockée peut porter les blancs d'une saisie ancienne — et
remplacer les trois formes d'absence par un repli nommé. `(invalide)` est un choix de libellé
d'affichage : la donnée absente ou blanche *existe* dans les bases réelles, et le DTO doit en
faire quelque chose de montrable plutôt que de propager un vide ambigu. Le repli entre
parenthèses a une vertu discrète — il ne peut pas être confondu avec un nom réel, là où une
chaîne vide disparaît dans l'interface et où un tiret ressemble à une donnée. Les trois formes
d'absence — la référence nulle, la chaîne vide, les blancs purs — convergent vers le même
repli : le consommateur du DTO n'a pas à connaître la différence entre les trois négligences
de saisie qui les ont produites.

Les cas cachés suivent l'énoncé à la lettre : le nom ordinaire qui traverse, les bordures qui
se rognent, et chacune des trois absences qui rend le repli — la garde `IsNullOrWhiteSpace`
étant précisément l'outil qui les unifie.

Le coût est négligeable ; la valeur est architecturale. La transposition est le réflexe DTO
complet : pour chaque champ qui sort du système, trois questions — doit-il sortir, sous quelle
forme normalisée, et que montre-t-on quand il manque ? La projection est l'endroit unique où
ces réponses vivent ; dispersées dans les contrôleurs ou laissées au sérialiseur, elles
finissent incohérentes d'une route à l'autre, et le client apprend à ne plus faire confiance
au contrat.
