# Explication

Un test dépendant de l'ordre est le défaut le plus corrosif d'une suite, parce qu'il ne ment pas une
fois : il ment différemment selon les jours. L'équipe apprend à relancer la chaîne « pour voir », puis
à ignorer le rouge, puis à fusionner malgré lui — et le jour où un rouge dit vrai, plus personne ne
l'écoute. Détecter ces tests mécaniquement, plutôt qu'à l'usure, est donc un geste d'hygiène d'équipe
autant qu'un geste technique.

**Pourquoi la détection passe par la divergence des verdicts et non par la lecture du code.** La cause
profonde — un état statique partagé, une base non remise à zéro, un cache de processus — est invisible
dans le journal et coûteuse à traquer dans le code. Mais son symptôme est parfaitement mesurable : le
même test, sur le même code, reçoit des verdicts différents selon la place qu'il occupe. C'est
pourquoi la fonction ne raisonne jamais sur l'ordre lui-même ; elle raisonne sur l'ensemble des
verdicts reçus par chaque nom. Deux verdicts distincts suffisent, quel que soit leur nombre total :
l'instabilité est une propriété qualitative, pas une fréquence.

**Pourquoi un test qui échoue partout est hors sujet.** L'intuition range volontiers tout rouge
récurrent dans le même sac. Or un test qui échoue dans toutes les exécutions est simplement cassé :
son verdict est stable, informatif, réparable. Le mélanger aux tests dépendants de l'ordre diluerait
le signal — on présenterait à l'équipe une liste où la moitié des entrées se répare en corrigeant le
code, l'autre en corrigeant l'isolation, sans moyen de les distinguer. La définition stricte protège
l'usage de la liste.

**Pourquoi les exécutions doivent couvrir le même ensemble de tests.** Si une campagne a filtré la
suite — exécution partielle, échantillonnage, arrêt prématuré — un test absent n'a pas « un autre
verdict » : il n'a pas de verdict. Le traiter comme divergent fabriquerait de faux positifs, et le
traiter comme stable masquerait de vrais instables. Refuser la comparaison est la seule position qui
ne devine rien ; c'est aussi ce que ferait un outil sérieux devant deux rapports incomparables.

**Pourquoi une exécution isolée rend une liste vide plutôt qu'un refus.** Une seule exécution est un
journal valide qui ne contient aucune preuve de divergence. Le refuser confondrait « données
insuffisantes pour accuser » et « données corrompues » ; rendre vide dit exactement ce que le journal
permet de dire, ni plus ni moins.

Le tri final n'est pas cosmétique : une sortie stable se compare d'une campagne à l'autre, s'inscrit
dans un rapport, se retrouve dans un différentiel. La transposition dépasse les tests : toute
détection d'instabilité — versions d'artefacts, résultats de déploiements — suit ce même schéma,
regrouper par identité puis compter les issues distinctes.
