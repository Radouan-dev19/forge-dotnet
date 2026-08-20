# Explication

« Au moins une fois » est la promesse la plus honnête qu'une messagerie puisse tenir : garantir
exactement une livraison à travers pannes et reconnexions exigerait un accord distribué à chaque
message, et le courtier choisit — raisonnablement — de relivrer dans le doute. La conséquence
structure tout consommateur sérieux : le doublon n'est pas un incident, c'est le régime nominal, et
la question n'est jamais « comment l'éviter » mais « que décider quand il arrive ». Cet exercice fige
cette décision en quatre verdicts, et leurs frontières portent chacune une leçon.

**Pourquoi le verdict ne se réduit pas à « connu ou inconnu ».** Le consommateur naïf saute tout
identifiant déjà vu. Il avale alors deux familles de messages légitimes : le message dont le
traitement précédent a **échoué** — sauté, il ne sera jamais traité, et l'échec passager devient une
perte définitive — et le message dont l'identifiant a été **recyclé** pour un autre contenu, qu'il
confond avec l'original. Les deux champs du registre existent pour ces deux familles : le statut
sépare l'échec du succès, l'empreinte sépare le même contenu d'un contenu divergent.

**Pourquoi la charge se vérifie avant le statut.** L'ordre des questions n'est pas décoratif. Si le
statut passait d'abord, un identifiant recyclé arrivant après un échec serait « retenté » — c'est-à-
dire appliqué — avec un contenu qui n'est pas celui que le registre décrit. La divergence de charge
est un défaut d'amont — un producteur qui recycle ses identifiants — et aucun statut ne la rachète :
le rejet explicite, bruyant, est la seule réponse qui fait remonter le vrai problème au lieu de
l'appliquer.

**Pourquoi le registre vide est un état et non une erreur.** Tout consommateur commence sans
historique, et chaque purge de rétention le ramène partiellement à cet état. Refuser le registre vide
rendrait le démarrage impossible ; le traiter comme « tout est inconnu » est exactement la sémantique
voulue — et c'est aussi pourquoi la rétention du registre est un paramètre d'exploitation sérieux :
un registre purgé trop tôt laisse revenir des doublons anciens en « premières livraisons ».

**Pourquoi le registre contradictoire se refuse.** Deux entrées pour le même identifiant signifient
que la source de vérité s'est dédoublée — deux consommateurs qui écrivent sans coordination, une
fusion de sauvegardes. Choisir l'une des deux entrées produirait un verdict plausible et
invérifiable ; le refus force la réparation de la comptabilité avant toute décision.

En entretien, ce dispositif se nomme le consommateur idempotent, et la question qui suit porte
presque toujours sur la file des messages morts : que faire du message rejeté pour divergence ? La
réponse tient dans le verdict lui-même — il est mis de côté avec sa raison, pour un humain, jamais
rejoué en boucle.
