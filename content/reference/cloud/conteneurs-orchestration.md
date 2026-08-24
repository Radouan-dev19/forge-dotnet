# Des conteneurs à l'orchestration

Un conteneur résout un problème précis : empaqueter une application avec ses dépendances
pour qu'elle tourne identiquement partout. `docker run` sur un poste de développeur suffit
tant qu'il n'y a qu'une seule instance, sur une seule machine, démarrée à la main. Dès qu'un
de ces trois mots cesse d'être vrai, un simple conteneur ne suffit plus.

## Ce que Docker seul ne répond jamais

**Plusieurs machines.** Une application qui doit survivre à la panne d'un serveur physique a
besoin d'instances sur des machines différentes — Docker seul ne sait pas répartir des
conteneurs sur un parc de serveurs, il sait en démarrer un sur la machine où on le lui
demande.

**Plusieurs instances qui se coordonnent.** Faire tourner cinq copies d'une API pour
absorber la charge pose immédiatement la question de la répartition du trafic entre elles,
et de ce qui se passe quand l'une plante — Docker seul ne redémarre pas automatiquement un
conteneur mort ailleurs que sur la machine où il vivait.

**Les mises à jour sans coupure.** Déployer une nouvelle version sans interrompre le service
suppose de démarrer les nouvelles instances, vérifier qu'elles répondent, basculer le trafic,
puis seulement arrêter les anciennes — dans cet ordre, jamais l'inverse. Un script qui fait
`docker stop` puis `docker run` cause une coupure, aussi brève soit-elle.

**La configuration et les secrets à l'échelle.** Cent conteneurs qui doivent tous recevoir la
même variable d'environnement, ou un secret qui doit être renouvelé sans reconstruire les
images, ne se gèrent pas fichier par fichier sur chaque machine.

## Pourquoi un script ne suffit pas non plus

Automatiser ces quatre problèmes avec des scripts est tentant, et c'est exactement ce que
faisaient les premières générations d'outils de déploiement. Le problème n'est pas
l'automatisation elle-même, c'est que ces problèmes sont **couplés** : un script qui répartit
la charge doit connaître l'état de santé de chaque instance pour ne pas y envoyer de trafic,
ce qui suppose une surveillance continue, ce qui suppose de redémarrer ce qui échoue, ce qui
retombe sur la mise à jour sans coupure. Chaque problème résolu isolément finit par redévelopper
un morceau des autres.

## Ce qu'un orchestrateur ajoute : une boucle, pas une commande

Un orchestrateur (Kubernetes est le plus répandu, mais Docker Swarm et Nomad existent aussi)
ne fonctionne pas par commandes ponctuelles : on lui décrit un **état désiré** — « cinq
instances de cette image, toujours » — et il maintient cet état en continu, en comparant ce
qui tourne réellement à ce qui a été demandé, et en corrigeant l'écart sans intervention. Une
instance qui plante est redémarrée parce que l'état désiré n'est plus atteint, pas parce
qu'une alerte a réveillé quelqu'un. C'est ce changement de paradigme — décrire un but plutôt
qu'une séquence d'actions — qui distingue un orchestrateur d'un script, aussi sophistiqué
soit-il.

## Ce qu'il faut retenir

Un conteneur isolé répond à « comment empaqueter » ; il ne répond à aucune des questions que
pose l'échelle : répartition, panne, mise à jour, configuration partagée. Ces questions sont
couplées, ce qui rend leur automatisation par scripts fragile à mesure qu'elle grandit. Un
orchestrateur les résout ensemble parce qu'il ne suit pas des commandes mais maintient un
état désiré — le sujet exact du guide suivant.
