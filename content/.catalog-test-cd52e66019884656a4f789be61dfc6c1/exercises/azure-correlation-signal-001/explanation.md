# Explication

Deux mesures corrélées — un compte d'erreurs, une latence au quatre-vingt-quinzième
percentile — et un verdict à trois états. Cette fonction est le cœur d'une règle d'alerte, et
ses trois décisions sont celles que tout tableau de bord d'exploitation doit prendre.

La priorité d'abord, et la question de l'énoncé : que laisse passer une alerte sur la latence
seule ? Les pannes *rapides*. Un service qui rejette instantanément toutes les requêtes — une
configuration cassée, un certificat expiré, une dépendance refusée — a une latence excellente :
les erreurs partent vite. L'alerte de latence dort paisiblement pendant que cent pour cent du
trafic échoue. C'est pourquoi les erreurs priment, inconditionnellement : une seule erreur
observée classe le signal `errors`, quelle que soit la latence — l'ordre des gardes transcrit
cette hiérarchie, et le cas caché « erreurs présentes, latence superbe » la verrouille. La
règle générale d'exploitation : les signaux de *défaillance* passent avant les signaux de
*dégradation*, parce que la défaillance sait se déguiser en excellente performance.

Le percentile ensuite, et il faut savoir dire pourquoi p95 plutôt que la moyenne : la moyenne
noie les extrêmes — mille requêtes rapides et cinquante catastrophiques donnent une moyenne
présentable, et les cinquante clients touchés n'existent pas dans le chiffre. Le
quatre-vingt-quinzième percentile dit « pour le client sur vingt le plus mal servi, voilà ce
que c'est » — c'est la métrique de l'expérience réelle, celle sur laquelle on écrit des
budgets. Le budget de sept cent cinquante millisecondes est ici une constante de contrat, et
sa frontière est *stricte* : au budget exact, le service est sain — le dépassement commence à
la milliseconde suivante, les deux cas cachés de part et d'autre figent l'inclusivité.

Les mesures négatives lèvent : un compte d'erreurs négatif est un bug de collecte, pas un état
du service, et le classement de l'absurde fabriquerait des rapports verts sur des données
fausses — le régime habituel des prédicats de politique du catalogue.

Trois verdicts nommés en sortie — pas un booléen : `errors` et `latency` ne déclenchent pas la
même réaction — l'un envoie vers les journaux d'exceptions, l'autre vers les profils et les
dépendances lentes — et le verdict porte l'aiguillage du diagnostic.

Le coût est constant. La transposition est la conception de toute règle d'alerte composée :
hiérarchiser défaillance avant dégradation, mesurer aux percentiles plutôt qu'à la moyenne,
border les budgets par des frontières testées, et refuser les mesures corrompues. Quatre
décisions par règle — et des astreintes qui dorment mieux.
