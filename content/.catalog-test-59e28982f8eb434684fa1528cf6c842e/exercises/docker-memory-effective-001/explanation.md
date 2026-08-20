# Explication

Le conteneur tué par manque de mémoire est l'incident d'exploitation le plus trompeur qui soit, parce
que la première question — « quelle était la limite ? » — n'a pas une réponse mais quatre candidates.
Le démon a une valeur par défaut, la plateforme impose un groupe de contrôle parent, le fichier de
composition déclare la sienne, et l'option de lancement peut encore en poser une autre. L'ingénieur
qui ne connaît que « sa » limite — celle du fichier qu'il a écrit — cherche l'erreur au mauvais étage.
Cet exercice fige le raisonnement de résolution, qui tient en deux règles et une nuance.

**Première règle : les plafonds ne se remplacent pas, ils s'empilent.** L'intuition venue de la
configuration applicative — la valeur la plus spécifique écrase les autres — est ici exactement
fausse. Chaque étage impose un plafond que les étages inférieurs ne peuvent pas dépasser : demander
deux gigaoctets dans la composition sous un groupe parent limité à un gigaoctet donne un gigaoctet,
sans erreur, sans avertissement, sans trace dans les fichiers du projet. C'est le rabattement, et son
caractère silencieux est ce qui le rend coûteux : la valeur écrite et la valeur appliquée divergent
sans que rien ne le signale. La limite effective est donc un minimum, pas une priorité.

**Seconde règle : en cas d'égalité, rapporter la source la plus proche du conteneur.** Cette règle ne
change pas la valeur — le minimum est le minimum — mais elle change l'action. Le diagnostic est écrit
pour un exploitant qui doit ajuster quelque chose : l'option de lancement se modifie en secondes, le
fichier de composition en minutes, le groupe parent exige un ticket à l'équipe de plateforme, la
configuration du démon touche toutes les charges de l'hôte. Désigner la source la plus proche, c'est
désigner le levier le moins cher qui suffit. Le départage arbitraire aurait produit des diagnostics
corrects et inutilisables.

**La nuance : zéro n'est pas l'infini.** Certains outils codent « sans limite » par zéro, d'autres
par l'absence de clé. Accepter les deux conventions dans la même chaîne finit toujours par appliquer
la mauvaise : un zéro pris pour un infini désactive un garde-fou, un infini pris pour un zéro tue le
conteneur au démarrage. L'exercice tranche : l'absence de contrainte est l'absence de la paire, et
zéro est refusé comme la faute de configuration qu'il est. De même, une source répétée est refusée
plutôt qu'écrasée — deux valeurs pour le même étage, c'est un fichier fusionné qui a mal tourné, pas
une préférence à deviner.

La transposition est directe : quotas de processeur, limites de descripteurs, tailles de journaux —
toute ressource bornée à plusieurs étages se résout par le même minimum, et tout diagnostic utile
nomme l'étage qui a agi.
