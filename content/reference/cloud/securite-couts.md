# Sécurité et coûts dans le cloud : les deux factures qu'on découvre trop tard

Un secret qui fuit et une facture qui explose partagent la même cause : une ressource dont
personne ne surveille l'usage réel. Les deux disciplines de ce guide — identité et coût — sont
ce qui distingue un compte de démonstration d'un système qu'on peut réellement mettre en
production.

## Le problème du secret en clair

Une chaîne de connexion à une base de données ou une clé d'API, écrite dans un fichier de
configuration ou une variable d'environnement en clair, est un secret qui peut fuiter par
n'importe quel canal qui touche ce fichier : un dépôt Git public par erreur, un journal de
déploiement, une capture d'écran de support. Le déplacer dans Key Vault résout le stockage,
mais pas entièrement le problème : il faut encore un moyen d'authentifier l'application
*auprès de* Key Vault pour récupérer le secret — et ce moyen d'authentification est lui-même
un secret, si on ne fait rien de plus.

## L'identité managée : fermer la boucle sans secret nulle part

Une **identité managée** donne à une ressource Azure (une App Service, une Function, un
cluster AKS) une identité gérée par Azure lui-même, que d'autres services Azure — Key Vault
en tête — reconnaissent nativement via Azure Active Directory. Aucun mot de passe, aucune
clé, aucun jeton ne transite ni ne se stocke pour établir cette confiance : l'identité est
prouvée par la plateforme, pas par un secret que le code devrait connaître. C'est la seule
manière de retirer complètement les identifiants de connexion du code source, plutôt que de
les déplacer d'un endroit en clair à un autre.

## Le moindre privilège, la règle qui rend l'identité utile

Donner à une identité managée un accès total à un abonnement pour lui permettre de lire un
seul secret annule une bonne partie du bénéfice : un composant compromis n'a alors plus besoin
de voler un mot de passe, il hérite directement de trop de droits. Le **moindre privilège**
consiste à accorder à chaque identité exactement les permissions nécessaires à sa tâche —
lecture d'un secret précis, écriture dans un seul conteneur de stockage — jamais plus, même
quand l'exception semble sans risque sur le moment.

## Les tags : rendre une facture lisible avant qu'elle explique un problème

Une facture cloud d'entreprise agrège des centaines de ressources sans hiérarchie visible tant
qu'aucune convention n'y est imposée. Un **tag** (paire clé-valeur attachée à une ressource :
`environment=production`, `team=paiements`, `project=forge`) rend possible de répondre à
« combien coûte ce projet » ou « combien coûte l'environnement de test » sans deviner à partir
des noms de ressources. Sans tags cohérents, la seule façon de comprendre une facture qui a
doublé est de tout inspecter à la main.

## Budgets et alertes : découvrir un dépassement en heures, pas en fin de mois

Un **budget** Azure fixe un seuil de dépense sur un abonnement ou un groupe de ressources, et
une **alerte de coût** prévient dès qu'un pourcentage de ce seuil est atteint — avant la fin du
cycle de facturation, pas après. Sans cette surveillance active, une ressource oubliée en
fonctionnement (une base de données de test jamais supprimée, un cluster dimensionné pour un
pic passé) ne se découvre qu'à la réception de la facture, quand il est déjà trop tard pour
limiter les dégâts.

## Ce qu'il faut retenir

Un secret en clair et une ressource sans tag ni budget partagent le même défaut : personne ne
surveille ce qui se passe entre deux vérifications manuelles. L'identité managée avec moindre
privilège élimine le secret comme surface d'attaque ; les tags et les budgets transforment une
facture illisible en signal exploitable avant l'incident, pas après.
