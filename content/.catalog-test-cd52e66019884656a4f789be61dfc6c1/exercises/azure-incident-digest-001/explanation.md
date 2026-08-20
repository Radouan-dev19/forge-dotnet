# Explication

Pendant un incident, l'information abonde et la réponse manque : le journal grossit d'une ligne par
minute pendant que la direction, le support et les équipes voisines posent quatre questions, toujours
les mêmes. Le brief est la discipline qui sépare ces quatre réponses du reste, et l'exercice encode
ses règles d'extraction — dont chacune corrige un biais réel des comptes rendus d'incident.

**Pourquoi la première occurrence et jamais la dernière.** Le biais le plus documenté des récits
d'incident est la réécriture : au fil des heures, l'impact se reformule en termes plus doux, le début
se rapproche du moment où l'équipe a réagi, et le propriétaire devient celui qui a fini par agir
plutôt que celui qui a été désigné. Extraire systématiquement la première occurrence fige la version
contemporaine des faits : le premier impact constaté est ce que les utilisateurs ont réellement subi,
la première attribution est l'engagement pris à chaud. La revue d'après-incident a précisément besoin
de cette version-là — l'écart entre le brief à chaud et le récit à froid est souvent la leçon.

**Pourquoi le début est la première trace observable, alerte ou impact.** Faire commencer l'incident
à la première action de l'équipe est flatteur et faux : la durée qui compte pour les utilisateurs
court depuis que quelque chose d'observable s'est produit. L'alterner selon ce qui arrive en
premier — parfois l'alerte précède l'impact constaté, parfois un humain constate avant les sondes —
donne le début le plus honnête que le journal permette. Le délai entre ce début et la première
atténuation est ensuite la métrique de réponse la plus regardée ; c'est pourquoi l'atténuation se
date, pendant que l'impact se cite.

**Pourquoi le brief incomplet se déclare au lieu de se maquiller.** Un journal sans impact nommé ou
sans attribution peut toujours produire quelque chose qui ressemble à un brief — des champs vides,
des tirets. Ce simulacre est pire que rien : il circule, il rassure, et personne ne voit que les deux
informations vitales manquent. La règle de l'exercice — déclarer l'incomplétude en nommant les
manques — transforme le défaut en tâche : nommer l'impact, désigner un propriétaire. Un incident sans
propriétaire de la prochaine action n'est pas géré, il est observé ; le brief qui l'avoue déclenche
la désignation, celui qui le cache la retarde.

**Pourquoi la chronologie se refuse quand elle décroît.** Des minutes qui reculent signalent un
journal recomposé après coup ou fusionné de deux sources : chaque extraction « première occurrence »
y perdrait son sens. Refuser vaut mieux que produire un brief dont les dates ne veulent rien dire —
la version chronologique du principe qui traverse tout ce parcours : une donnée corrompue se signale,
elle ne s'interprète pas.

La transposition : le même extracteur — premières occurrences, vitaux déclarés, chronologie
vérifiée — sert les revues de déploiement raté, les analyses de dégradation et tout ce qui commence
par « raconte-moi ce qui s'est passé » avec un journal pour seule source.
