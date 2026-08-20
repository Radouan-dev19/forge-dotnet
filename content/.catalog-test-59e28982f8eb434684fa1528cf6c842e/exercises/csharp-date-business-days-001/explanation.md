# Explication

Compter les jours ouvrés d'une plage se fait ici par le chemin le plus honnête : visiter chaque
date et demander son jour de semaine. Cette approche mérite d'être défendue, car il en existe
une plus rapide — et le choix entre les deux est le vrai sujet.

La version arithmétique calcule le nombre de semaines complètes, multiplie par cinq, puis
corrige pour les jours restants selon le jour de départ. Elle est en temps constant et
elle est célèbre pour être fausse du premier coup : la correction des extrémités cache une
demi-douzaine de cas selon que la plage commence ou finit en week-end, et chaque correction de
bug en introduit une autre. La version par parcours est linéaire dans la longueur de la plage —
parfaitement acceptable pour des plages humaines de jours ou de mois — et sa correction se lit
d'une phrase : chaque date est comptée si son jour n'est ni samedi ni dimanche. Choisir la
version qu'on peut prouver plutôt que celle qui impressionne est une décision d'ingénierie, pas
une paresse ; le jour où le profil montre des plages de plusieurs siècles interrogées mille fois
par seconde, l'arithmétique se réintroduira, adossée à des tests différentiels contre la version
lente.

La boucle elle-même contient deux précisions de contrat. Les bornes sont *incluses* toutes les
deux — `current <= end` — et l'énoncé le dit : un lundi seul compte pour un. Et l'intervalle
inversé rend zéro, par le simple fait qu'une boucle dont la condition est fausse d'emblée ne
tourne pas : aucun cas spécial, la convention est portée par la structure. Les cas cachés se
placent là où les erreurs de ce domaine vivent : un samedi seul rend zéro, une plage du vendredi
au lundi rend deux — elle traverse le week-end sans le compter —, et une semaine pleine rend
cinq.

Le filtre s'écrit avec les motifs `is not ... and not ...` sur l'énumération `DayOfWeek`, la
forme moderne qui se lit comme la règle métier elle-même. `DateOnly` est le bon type : pas
d'heure, pas de fuseau, donc aucune des ambiguïtés de minuit qui polluent les calculs faits avec
des dates horodatées.

L'énoncé exclut les jours fériés, et cette exclusion est une leçon en soi : les fériés dépendent
du pays, de l'année et parfois de l'accord d'entreprise — c'est une *donnée*, pas une règle
calculable. La transposition à retenir tient là : séparer ce qui se calcule (le week-end,
universel) de ce qui se configure (les fériés, locaux), et refuser poliment de coder en dur ce
qui appartient à une table.
