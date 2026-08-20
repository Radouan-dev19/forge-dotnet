# Explication

L'analyse des points chauds est la réponse mesurée à la question la plus angoissante du métier :
par où entrer dans une base qu'on ne connaît pas ? Les deux réflexes naturels échouent chacun à leur
manière, et le produit churn-complexité existe précisément parce que leurs échecs sont
complémentaires.

**Pourquoi ni la complexité seule ni le churn seul.** Le fichier le plus complexe du dépôt est
souvent un fossile : un analyseur syntaxique écrit il y a six ans, effrayant à lire et jamais
touché — le comprendre coûte cher et ne prépare aucun travail réel. Le fichier le plus modifié est
souvent trivial : un registre de configuration, un fichier de constantes que chaque fonctionnalité
effleure — le lire n'apprend rien. Le danger vit à l'intersection : le code **difficile** qu'on
touche **tout le temps**, celui où chaque modification est un risque et où la prochaine tâche de
l'équipe passera probablement. Le produit des deux mesures pointe exactement là, et l'historique du
dépôt fournit les deux gratuitement — c'est l'analyse au meilleur rapport information sur effort de
tout le répertoire legacy.

**Pourquoi le podium et pas l'inventaire.** La liste complète des fichiers classés existe dans
l'outil ; le rapport, lui, rend trois noms. La différence est comportementale : un inventaire de deux
cents entrées se transforme en réunion de priorisation — c'est-à-dire en report — tandis qu'un podium
se transforme en action : ces trois fichiers reçoivent la lecture approfondie, les tests de
caractérisation, la vigilance de revue. La limite arbitraire est le prix de l'exécution, et le
rapport se rejoue chaque trimestre : le podium bouge, et son mouvement raconte la dette en train de
se déplacer.

**Pourquoi le départage et l'entier large, encore.** Deux fichiers au même score sont fréquents dans
les bases générées ou symétriques ; sans départage, deux exécutions rendraient deux podiums, et le
différentiel trimestriel — la vraie valeur du rapport — deviendrait illisible. Quant au produit, il
déborde le trente-deux bits dès que les deux mesures sont grandes ensemble — précisément sur les
monstres que l'analyse cherche : le débordement inverserait le classement en silence, reléguant le
pire fichier au fond du rapport. L'arithmétique large n'est pas de la prudence décorative, elle est
la condition pour que l'outil fonctionne sur les cas qui le justifient.

**Ce que le podium prépare.** Les trois fichiers désignés ne se remanient pas d'office : ils se
**caractérisent** d'abord — le geste de la semaine seize du socle — puis se remanient sous filet.
L'analyse des points chauds est le premier maillon de la chaîne legacy : mesurer où lire,
caractériser ce qu'on a lu, remanier ce qu'on a caractérisé.

En entretien, le terme est hotspot analysis, et la question type est celle de l'énoncé — « trois
jours pour entrer dans une base inconnue, vous commencez où ? ». La réponse mesurée bat toujours la
réponse héroïque.
