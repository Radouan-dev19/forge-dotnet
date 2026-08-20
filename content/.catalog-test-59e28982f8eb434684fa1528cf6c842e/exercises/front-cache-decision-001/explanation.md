# Explication

La stratégie « servir périmé pendant la revalidation » existe pour résoudre une tension que tout
cache connaît : la donnée la plus rapide à servir est celle qu'on a déjà, mais elle vieillit. Plutôt
que de choisir entre servir vite et servir juste, cette stratégie découpe le temps en trois régimes.
Tant que l'entrée est fraîche, on la sert sans réfléchir. Passé un premier seuil, on la sert quand
même, immédiatement, tout en déclenchant en arrière-plan une revalidation qui la rafraîchira pour la
prochaine fois. Passé un second seuil, on refuse de la servir : elle est trop vieille, il faut
attendre une réponse à jour. Cette fenêtre intermédiaire est le cœur de la valeur : elle offre à
l'utilisateur une réponse instantanée pendant que le système se met discrètement à jour.

Tout l'exercice se joue sur le placement exact des bornes, et c'est pour cette raison que les cas
cachés visent les instants pile sur un seuil. Un âge égal à `staleAfter` bascule déjà en
revalidation : la borne basse est inclusive côté périmé. Un âge égal à `expireAfter` bascule en
expiré : la borne haute est elle aussi franchie dès l'égalité. Se tromper d'un cran, écrire un
inférieur ou égal là où il fallait un inférieur strict, ne se voit sur aucun cas grossier ; cela ne
se révèle qu'à l'instant exact du seuil. C'est le genre d'erreur qui passe tous les tests
approximatifs et provoque, une fois en production, une réponse servie une seconde de trop ou refusée
une seconde trop tôt, de façon impossible à reproduire à la main.

L'âge négatif mérite son propre traitement. Entre la machine qui a écrit l'entrée et celle qui la
lit, les horloges ne sont jamais parfaitement d'accord ; un instant de stockage légèrement dans le
futur produit un âge négatif. La règle veut qu'on le traite comme frais, ce qui tombe naturellement
puisqu'un nombre négatif est inférieur à un seuil positif. L'important est de ne pas ajouter un
garde-fou maladroit qui déclarerait périmée une entrée simplement parce que son âge sort de
l'intervalle par le bas.

La validation des seuils protège contre un contrat impossible. Si `staleAfter` dépassait
`expireAfter`, la zone de revalidation n'existerait pas et le code produirait des verdicts que
personne ne saurait interpréter. Refuser cette configuration à l'entrée, plutôt que de deviner une
intention, place l'erreur là où elle appartient : chez l'appelant qui a fourni des bornes
contradictoires. De même, une entrée mal formée n'a pas d'âge calculable ; rendre un verdict au
hasard cacherait un bogue de sérialisation en amont.

Le coût est constant : une lecture, quelques comparaisons entières. La transposition va bien au-delà
du cache navigateur. Les mêmes trois régimes gouvernent un jeton d'accès qu'on renouvelle avant
qu'il n'expire, un certificat qu'on remplace pendant sa période de grâce, ou une configuration qu'on
recharge sans bloquer le service. Partout, la discipline est la même : nommer les bornes avec
précision et décider ce que signifie l'instant exact où on les atteint.
