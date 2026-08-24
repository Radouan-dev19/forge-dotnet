# Kubernetes : les concepts qui comptent

Kubernetes a un vocabulaire dense, mais chaque objet répond à un problème précis. Les
apprendre par le problème qu'ils résolvent, plutôt que par une liste à mémoriser, permet de
lire n'importe quel cluster sans jamais en avoir déployé un.

## Pod : la plus petite unité qu'on déploie

Un **Pod** regroupe un ou plusieurs conteneurs qui partagent le même réseau et le même
stockage éphémère — dans la grande majorité des cas, un Pod contient un seul conteneur, et
les deux mots se confondent en pratique. Kubernetes ne planifie jamais un conteneur seul : il
planifie toujours un Pod. Un Pod est **jetable** — il n'a pas d'identité stable, ne se répare
pas, se remplace.

## Deployment : la boucle qui maintient un nombre de Pods

Un **Deployment** décrit un état désiré — « trois copies de cette image de conteneur,
toujours » — et Kubernetes le fait respecter en continu : un Pod qui meurt est remplacé par
un nouveau, pas redémarré, parce que le Pod original n'existe plus. C'est le Deployment,
et non le Pod, qui porte la logique de mise à jour progressive : remplacer les anciens Pods
par les nouveaux un par un, en vérifiant que chaque nouveau répond avant de couper le
suivant.

## Service : une adresse stable sur des Pods instables

Puisque des Pods naissent et meurent en permanence, chacun avec sa propre adresse IP interne,
rien ne doit jamais s'adresser directement à un Pod. Un **Service** donne une adresse réseau
stable qui route vers l'ensemble des Pods correspondant à une étiquette (label) donnée,
quelle que soit leur identité du moment. Un client s'adresse au Service ; le Service choisit
un Pod vivant parmi ceux qui correspondent — c'est ce niveau d'indirection qui rend le
remplacement d'un Pod invisible pour qui le consomme.

## Namespace : cloisonner sans multiplier les clusters

Un **Namespace** partitionne un cluster en espaces logiquement isolés — équipes, environnements
(staging, production), ou applications distinctes — qui partagent le même matériel sous-jacent
sans se marcher dessus dans les noms de ressources ni dans les quotas alloués. Un cluster de
développement local n'a souvent besoin que du Namespace `default` ; un cluster partagé par
plusieurs équipes en a presque toujours plusieurs.

## ConfigMap et Secret : séparer la configuration du code

Un **ConfigMap** porte de la configuration non sensible (URL d'un service, niveau de journal),
qu'un Pod peut lire sans que l'image du conteneur ait besoin d'être reconstruite pour un
changement de valeur. Un **Secret** porte la même idée pour des données sensibles (mot de
passe, jeton) — encodé, pas chiffré par défaut dans une installation minimale, ce qui justifie
qu'un cluster de production le couple presque toujours à un coffre externe (comme Azure Key
Vault) plutôt que de lui faire confiance seul.

## ReplicaSet : le mécanisme derrière le Deployment

Un **ReplicaSet** est l'objet qui compte réellement les Pods et en recrée quand le nombre
observé descend sous le nombre désiré ; un Deployment en crée et en gère un à chaque mise à
jour. En pratique, on manipule presque toujours des Deployments directement et on ne consulte
un ReplicaSet que pour comprendre ce qui se passe pendant un déploiement en cours.

## La boucle de réconciliation, le principe qui relie tout

Chacun de ces objets suit le même mécanisme : Kubernetes observe en continu l'état réel du
cluster, le compare à l'état désiré déclaré dans ces objets, et corrige l'écart — sans
séquence d'actions à écrire, sans alerte à surveiller manuellement. Comprendre cette boucle
unique explique pourquoi un Pod supprimé à la main par erreur réapparaît immédiatement : ce
n'est pas un comportement spécial, c'est la même réconciliation qui tourne toujours.

## Ce qu'il faut retenir

Le Pod est jetable, le Deployment le maintient, le Service l'adresse malgré son instabilité,
le Namespace le cloisonne, ConfigMap et Secret séparent sa configuration de son image — et
tout ce vocabulaire n'est qu'une déclinaison d'un seul mécanisme : observer, comparer,
corriger, en boucle.
