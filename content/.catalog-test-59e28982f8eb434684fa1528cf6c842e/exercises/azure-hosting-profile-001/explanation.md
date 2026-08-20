# Explication

Les choix d'hébergement se prennent trop souvent par affinité — l'équipe connaît tel service, donc
tout y va — et se paient en facture ou en friction de livraison. La table de cet exercice n'a rien de
sorcier ; sa valeur est dans l'ordre des questions et dans l'obligation de motiver, deux disciplines
qui survivent aux évolutions du catalogue des fournisseurs.

**Pourquoi le rythme se décide avant tout le reste.** Le rythme de la charge détermine le modèle de
facturation qui lui convient, et la facturation est la conséquence la plus difficile à corriger après
coup. Une charge réveillée par des événements et endormie le reste du temps paie, sur un hébergement
à instance permanente, vingt-trois heures d'inactivité par jour — un surcoût structurel qu'aucun
réglage ne rattrape. L'inverse est vrai aussi : une charge continue sur un modèle à l'événement paie
chaque requête au détail. C'est pourquoi le profil événementiel court-circuite la table : la
facturation à l'événement pour du code, la mise à zéro entre deux réveils pour un conteneur. La
distinction entre les deux n'est pas cosmétique — l'artefact conteneurisé embarque son exécution et
ses dépendances, ce que le modèle à l'événement pur ne sait pas héberger sans le déballer.

**Pourquoi la livraison départage les rythmes continus.** Une fois le rythme réglé, la question
n'est plus « où tourner » mais « comment livrer sans interrompre ». Plusieurs révisions actives avec
répartition de trafic — l'exposition progressive, le retour arrière instantané — sont le métier de
l'hébergement à révisions quand l'artefact est un conteneur, et celui des emplacements d'échange
quand la plateforme fournit l'exécution. À version unique, ces mécanismes sont du poids mort : la
plateforme d'exécution gérée suffit, et le conteneur isolé y trouve aussi sa place. Le piège que la
question finale de l'énoncé nomme — dimensionner pour une capacité « prévue pour plus tard » — est le
sur-achat classique : la capacité inutilisée se paie en complexité d'exploitation immédiate contre un
besoin hypothétique, alors que la migration au moment du vrai besoin est un chantier borné.

**Pourquoi la raison fait partie de la réponse.** Un choix d'hébergement se révise quand le profil
change — la charge continue devient événementielle, la version unique devient multiple. La raison
attachée à la recommandation dit exactement quelle évolution du profil doit rouvrir la décision : une
recommandation motivée par la facturation à l'événement se réexamine quand le rythme change, pas
quand l'équipe change d'avis. Sans la raison, chaque évolution rouvre tout.

**Pourquoi les refus restent stricts.** Un attribut deviné produirait une recommandation plausible
sur un profil que personne n'a décrit — et les décisions d'infrastructure plausibles mais infondées
sont celles qu'on découvre en facture, des mois plus tard.

La transposition : la même grille — rythme puis artefact puis livraison, raison obligatoire —
fonctionne pour choisir une base de données, une file ou un cache géré. Le fournisseur change, la
discipline non.
