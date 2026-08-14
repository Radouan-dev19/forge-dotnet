# Explication

Deux modèles d'hébergement, deux conditions, et une règle de décision dont la forme même est
un principe d'architecture : le choix le plus capable n'est pas le choix par défaut.

La règle se lit dans la conjonction : le modèle à révisions de conteneur ne se choisit que si
le besoin de ces révisions *existe* — bascules progressives entre versions, partage de trafic,
retour arrière par révision — *et* si l'artefact est *déjà* conteneurisé. Tout le reste va au
modèle managé classique, y compris les cas où une seule des deux conditions est vraie — et ces
deux cas intermédiaires sont les plus instructifs. Un besoin de révisions sans conteneur
existant signifierait conteneuriser *pour* accéder à la capacité : un coût certain
aujourd'hui — image à construire, pipeline à équiper, registre à gérer — pour un besoin
peut-être réel ; la règle répond « pas encore » — le jour où l'artefact sera conteneurisé pour
ses propres raisons, la conjonction s'ouvrira. Un conteneur existant sans besoin de révisions,
symétriquement, tourne très bien sur le modèle managé, qui accepte les conteneurs sans exiger
d'en gérer la sophistication.

La question de l'énoncé — que coûte une capacité prévue « pour plus tard » ? — a une réponse
comptable : le prix plein, dès aujourd'hui. La capacité inutilisée ne coûte pas rien : elle
coûte sa complexité d'exploitation — plus de concepts à maîtriser pour l'astreinte, plus de
configuration à maintenir, plus de surface d'erreur — et cette facture tombe chaque jour, que
la capacité serve ou non. L'inverse — commencer simple, migrer quand le besoin se prouve — paie
la migration une fois, le jour où elle est justifiée par un besoin réel et mesuré. Le principe
a un nom dans les manuels — on n'en aura pas besoin avant d'en avoir besoin — et cette règle de
deux booléens en est la version exécutable.

Le domaine d'entrée est fini — quatre combinaisons, écrites avant le code comme l'énoncé
l'exige, couvertes par les cas — et l'exhaustivité triviale est assumée : la valeur de
l'exercice est l'argumentaire de chaque feuille, celui que la question d'entretien associée
fait dérouler à voix haute.

Le coût est constant. La transposition est la structure de toute décision d'infrastructure :
identifier la capacité différenciante du choix coûteux, exiger la *preuve du besoin* et la
*préexistence du prérequis* avant de le retenir, et laisser le défaut simple gagner tous les
autres cas. Une grille de décision écrite ainsi se réévalue sans drame quand les conditions
changent — c'est sa qualité première : elle nomme ce qui la ferait basculer.
