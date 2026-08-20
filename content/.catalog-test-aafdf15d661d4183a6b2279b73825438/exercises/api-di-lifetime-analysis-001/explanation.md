# Explication

Les durées de vie d'injection ont une réputation de sujet d'entretien alors qu'elles sont un sujet
d'incident : les erreurs de durée de vie ne cassent pas au démarrage, elles corrompent en charge.
L'exercice fige la cascade de décision, et l'ordre de cette cascade est tout son contenu — chaque
règle placée trop bas devient un bogue précis et documenté.

**Pourquoi l'état prime sur le coût.** L'intuition économique — « c'est cher à construire, donc une
seule instance » — est la première cause de services de requête devenus partagés. Un service qui
porte des données propres à la requête et vit en instance unique mélange les utilisateurs : le panier
de l'un apparaît chez l'autre, sous charge seulement, jamais en développement. Le coût de
construction est un vrai critère, mais c'est le **dernier** : il ne départage que les services que ni
l'état ni les consommations n'ont déjà classés. Une cascade qui met le coût en premier optimise des
millisecondes en fabriquant des fuites de données.

**Pourquoi la dépendance captive a sa propre règle.** Un service à durée unique qui consomme un
service de requête reçoit une instance à sa construction — la première — et la garde pour toujours.
Le contexte de données de la requête inaugurale survit ainsi des heures, servant des données
périmées, gardant une connexion, échouant de façon incompréhensible. Le conteneur d'injection de la
plateforme refuse d'ailleurs cette configuration à la validation quand on la lui demande ; la règle
de l'exercice encode ce refus au stade de l'analyse, avant même l'enregistrement. D'où sa position :
un consommateur de service de requête est au moins de durée de requête, quel que soit son coût.

**Pourquoi le profil irréconciliable se signale au lieu de se résoudre.** L'état partagé demande la
durée unique ; la consommation d'un service de requête l'interdit. Choisir l'une des deux durées
« quand même » produirait soit un état partagé recréé à chaque requête — donc plus partagé du tout —
soit une dépendance captive. La seule recommandation honnête est le conflit : ce profil décrit un
service à découper — l'état partagé d'un côté, la consommation de requête de l'autre — pas un
service à enregistrer. Une analyse qui répond toujours quelque chose de plausible est plus dangereuse
qu'une analyse qui sait dire « ce que vous décrivez est le problème ».

**Pourquoi la raison accompagne la durée.** La même durée recommandée pour deux raisons différentes
appelle deux vigilances différentes : la durée unique par coût se revérifie quand le service gagne un
état ; la durée de requête par consommation se revérifie quand la consommation disparaît. La raison
est la partie de la recommandation qui survit aux évolutions du service.

La transposition : devant tout enregistrement douteux dans une revue, poser les trois questions dans
l'ordre de la cascade — quel état, quelles consommations, quel coût — retrouve la recommandation en
trente secondes, et surtout la justifie.
