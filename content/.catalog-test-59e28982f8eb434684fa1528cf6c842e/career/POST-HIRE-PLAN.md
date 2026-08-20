# Plan 30/60/90 d'un premier poste .NET

Les quatre-vingt-dix premiers jours d'un premier poste ne servent pas à briller : ils servent à
devenir fiable, visiblement et progressivement. Ce plan propose des gestes concrets par période,
avec une règle qui prime sur tout le reste : **les attentes réelles de votre équipe remplacent ce
plan dès qu'elles le contredisent.** Demandez-les explicitement la première semaine.

## Jours 1 à 30 — comprendre avant de changer

- Obtenez les accès minimaux et notez chaque étape : votre parcours d'arrivée est la première
  contribution possible — un correctif à la documentation d'accueil se livre dès la deuxième
  semaine et ne casse rien.
- Cartographiez un parcours métier de bout en bout : quelle requête entre, quels services elle
  traverse, où elle est persistée. Dessinez-le, faites-le corriger par un ancien.
- Faites tourner build, tests et déploiement localement avant de toucher au code. Celui qui sait
  livrer inspire plus confiance que celui qui sait coder.
- Livrez un premier correctif borné — un vrai ticket, petit — en suivant le processus complet :
  branche, tests, revue, livraison.
- Tenez un journal quotidien : ce qui a été appris, ce qui bloque, les questions posées et leurs
  réponses. C'est votre matière pour les points avec le manager.
- En revue de code, commencez par recevoir : lisez chaque remarque comme une information sur les
  standards de l'équipe, pas comme un verdict sur vous.

## Jours 31 à 60 — élargir le périmètre

- Prenez des tickets de taille croissante, en annonçant vos estimations et en les confrontant au
  réel : l'écart assumé entre estimé et réalisé construit la confiance plus vite que l'optimisme.
- Ajoutez des tests là où vos tickets vous emmènent : chaque zone touchée doit être mieux couverte
  après votre passage qu'avant.
- Commencez à donner en revue : d'abord des questions (« que se passe-t-il si cette entrée est
  vide ? »), ensuite des remarques — en distinguant toujours ce qui est prouvé de ce qui est une
  préférence, le réflexe travaillé dans la piste senior.
- Comprenez la production : où vivent les journaux, qui est alerté quand quelque chose casse,
  comment on revient en arrière. Assistez à un déploiement avant d'en conduire un.
- Identifiez le point chaud du code — le fichier que tout le monde modifie en soupirant — sans
  proposer encore de le réécrire. La cartographie churn-complexité de la piste senior s'applique
  telle quelle ici.

## Jours 61 à 90 — livrer et proposer

- Livrez une petite fonctionnalité de bout en bout : conception courte écrite, découpage, tests,
  revue, déploiement, vérification en production.
- Prenez votre part du support : reproduire un bogue signalé, le corriger avec un test de
  non-régression, répondre à celui qui l'a signalé. La méthode des DebugLabs — symptôme, hypothèse,
  preuve, prévention — est exactement celle qu'on attend.
- Proposez **une** amélioration mesurée, choisie petite : un test manquant sur un chemin critique,
  une alerte qui manque, une documentation d'arrivée. Une proposition livrée vaut dix suggestions.
- Préparez le bilan des quatre-vingt-dix jours avec votre journal : ce qui est acquis, ce qui reste
  fragile, ce que vous visez au semestre suivant — et demandez un retour aussi précis que celui que
  vous donnez.

## La piste senior en ligne de mire

Les gestes seniors ne s'exigent pas d'un premier poste, mais ils se repèrent tôt : lire une trace
avant d'accuser un service, chercher le temps propre plutôt que le span le plus long, trier une
revue selon la preuve plutôt que selon l'insistance, refuser un découpage sans force qui pousse.
Les nommer avec les termes du métier — dans les revues, les incidents, les discussions
d'architecture — est précisément ce que la piste senior de ce programme entraîne.

## Ce que ce plan n'est pas

Il ne remplace ni l'accompagnement d'un manager ni les rituels de l'équipe, et il ne garantit ni
confirmation de période d'essai ni évolution : il rend vos premiers mois lisibles, pour vous et pour
ceux qui vous accueillent.
