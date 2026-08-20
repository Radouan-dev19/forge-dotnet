# Économiser les tokens sans perdre en qualité

Réduire la consommation de tokens n'est pas de l'avarice : un contexte court et pertinent produit de
meilleures réponses qu'un contexte gonflé, en plus de coûter moins. Ce guide classe les techniques
par rendement, de celle qui change tout à celles qui affinent.

## D'abord, savoir ce qui consomme

Par ordre de gourmandise décroissante, dans une session de développement typique : les fichiers
collés en entier alors que dix lignes suffisaient ; les sorties brutes d'outils — logs, traces de
build, résultats de tests verbeux — recopiées telles quelles ; l'historique de conversation qui
s'accumule et se refacture à chaque échange (chaque message renvoie toute la fenêtre) ; les allers
retours de correction sur une réponse partie dans la mauvaise direction ; enfin vos instructions
elles-mêmes, marginales en comparaison. Retenez la hiérarchie : **on économise sur ce qu'on montre
au modèle, pas sur ce qu'on lui demande**.

## Technique 1 — Montrer l'extrait, nommer le reste

Le réflexe le plus rentable : au lieu de coller un fichier, donnez le chemin et les lignes utiles,
et laissez l'outil lire ce dont il a besoin. Les assistants intégrés au dépôt savent ouvrir un
fichier ciblé ; un collage massif les prive de ce choix et vous facture des centaines de lignes que
la question ne concernait pas. Même logique pour les erreurs : la première erreur de compilation et
ses cinq lignes de contexte valent mieux que six pages de sortie — les erreurs suivantes sont
souvent des conséquences de la première.

## Technique 2 — Stabiliser le début, varier la fin

Les fournisseurs mettent en cache le **préfixe** de la requête : si le début de votre conversation
(instructions système, consignes du dépôt, premiers échanges) est identique d'un appel à l'autre, il
est refacturé à une fraction du prix et traité plus vite. Conséquence pratique : mettez le stable en
tête — les règles du projet, le style attendu — et ne les reformulez jamais en cours de route, car
modifier un mot du préfixe invalide le cache de tout ce qui suit. C'est aussi pourquoi les fichiers
de consignes de dépôt (guide suivant) sont économiquement rentables : ils sont envoyés à chaque
session, donc presque toujours servis depuis le cache.

## Technique 3 — Clore les sessions au bon moment

Une conversation qui a réglé un sujet porte le poids de ce sujet pour toujours. Deux signaux qu'il
est temps d'ouvrir une session neuve : vous changez de tâche, ou vous corrigez l'assistant pour la
troisième fois sur la même incompréhension — l'historique fautif re-contamine chaque réponse. Avant
de fermer, demandez un résumé de trois phrases des décisions prises et collez-le en ouverture de la
suivante : vous transportez la conclusion, pas le cheminement. La compaction automatique de certains
outils fait la même chose ; savoir la déclencher vous-même la rend prévisible.

## Technique 4 — Déporter les grosses lectures

Quand une question exige de parcourir vingt fichiers — « où est gérée la pagination dans ce dépôt ? »
— la pire méthode est de les faire défiler dans votre conversation principale. Déléguez la fouille à
un sous-agent (guide dédié plus loin) : il consomme les vingt fichiers dans **son** contexte jetable
et ne vous rapporte que la conclusion. Votre session principale reste légère, et c'est elle qui doit
durer.

## Technique 5 — Contraindre la sortie

La sortie coûte plus cher que l'entrée et se relit encore plus cher. Dites la forme attendue :
« uniquement le diff », « la fonction corrigée, sans réécrire le fichier », « trois options en une
ligne chacune, pas d'implémentation ». Interdisez explicitement ce que vous ne voulez pas — les
réécritures complètes de fichiers pour un changement de deux lignes sont le gaspillage le plus
courant. Une réponse courte et juste vaut dix réponses longues à trier.

## Technique 6 — Adapter le modèle à la tâche

Renommer, formater, générer du squelette, écrire un message de commit : un petit modèle rapide fait
cela aussi bien pour dix fois moins. Concevoir, déboguer un problème retors, relire de la sécurité :
prenez le grand modèle et donnez-lui un contexte soigné. L'erreur classique est l'uniforme — tout au
grand modèle par confort, ou tout au petit par économie ; le professionnel route.

## Mesurer, sinon rien

Chaque outil affiche quelque part sa consommation — par requête, par session ou par mois. Regardez
la répartition entrée/sortie/cache une fois par semaine : une entrée énorme signale des collages à
remplacer par des lectures ciblées ; un cache faible signale un préfixe instable ; une sortie énorme
signale des réponses à contraindre. Trois chiffres, trois diagnostics — le même réflexe que pour la
performance d'une application : on n'optimise pas ce qu'on ne mesure pas.
