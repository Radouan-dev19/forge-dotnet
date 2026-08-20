# Explication

Entre « la saga a réussi » et « la saga a échoué », il existe un troisième état que les tableaux de
bord écrasent et que les astreintes paient : la compensation **en cours ou bloquée**. Une saga dont
l'échec est survenu il y a trois heures peut être revenue au repos — tout est défait, personne n'a
rien à faire — ou figée à mi-compensation — un débit existe sans commande, et chaque minute qui passe
est une minute d'incohérence visible. La même étiquette « échec » couvre les deux, et c'est
exactement le genre d'ambiguïté qu'un système sérieux refuse. Cet exercice fabrique le verdict qui la
lève.

**Pourquoi le verdict se calcule par confrontation d'ensembles.** Le journal raconte trois choses
mêlées : ce qui a été accompli, où la saga s'est arrêtée, ce qui a été défait. Le verdict n'a besoin
que de la confrontation des deux extrémités — les étapes accomplies avant l'échec contre les étapes
compensées après. Si la seconde couvre la première, la saga est au repos ; sinon, la différence
**est** le travail restant, et le verdict peut le nommer au lieu de le résumer. C'est le même
principe que le brief d'incident : la sortie d'un diagnostic est une action, pas un adjectif.

**Pourquoi le blocage nomme la dernière étape debout.** La compensation procède en ordre inverse —
c'est la discipline vue avec le plan de compensation — donc l'étape que la reprise doit défaire en
premier est la **dernière** accomplie encore non compensée. Nommer la première ferait reprendre la
compensation par le mauvais bout : l'astreinte annulerait la commande pendant que le débit tient
toujours, exactement l'état intermédiaire que l'ordre inverse existe pour éviter. Le détail du choix
de nommage encode donc toute la sémantique de reprise.

**Pourquoi la compensation sans échec est un refus et non une curiosité.** Un journal qui montre une
étape défaite sans qu'aucun échec ne soit survenu raconte une histoire impossible : la compensation
n'a pas de déclencheur. Deux explications existent — un journal recomposé de deux sagas, ou un
orchestrateur qui compense par erreur — et les deux exigent une investigation, pas un verdict. De
même pour le second échec : la saga s'arrête au premier, et un journal qui en montre deux mélange
deux exécutions. La règle générale de la piste se répète : on ne qualifie que ce qui est cohérent.

**Le cas de l'échec immédiat éclaire la définition.** Un journal réduit à `fail` n'a aucune étape
accomplie : l'ensemble à défaire est vide, la couverture est vacuously complète, le verdict est
`compensated`. Ce n'est pas une pirouette logique — c'est la bonne réponse opérationnelle : rien
n'est debout, personne n'a rien à faire.

En entretien, ce sujet s'articule avec le vocabulaire de la saga orchestrée — l'orchestrateur tient
précisément ce journal — et la question type est celle de l'ambiguïté initiale : « échec » veut dire
quoi, exactement, trois heures après ?
