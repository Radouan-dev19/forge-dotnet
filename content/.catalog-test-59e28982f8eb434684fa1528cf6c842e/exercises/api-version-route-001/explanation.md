# Explication

Résoudre la version d'une route paraît être une extraction de chaîne ; c'est en réalité une
décision à trois issues, et c'est la troisième — la version demandée mais inconnue — qui porte
tout l'enseignement, car c'est elle qu'on rate en la fondant dans les deux autres.

Les trois situations sont distinctes par leur *intention*. Aucun segment de version signifie
« le client n'a rien demandé » : on sert la version par défaut, choix de politique du serveur — la
plus récente, souvent. Un segment de version reconnu signifie « le client demande cette
version-ci » : on la sert. Un segment de version *présent mais absent de la liste* signifie « le
client demande une version qui n'existe pas » — une v9 sur une API qui s'arrête à v3 : c'est une
erreur explicite du client, et la ramener silencieusement à la version par défaut serait un
mensonge. Il croirait parler à la v9, recevrait les réponses de la v3, et le décalage se
manifesterait par des comportements inexplicables. Le verdict `unsupported` rend l'erreur visible,
là où le repli la cache. Confondre « pas de version » et « mauvaise version » est l'erreur
centrale que le cas caché à la version inconnue débusque.

La mécanique sépare l'analyse de la décision. L'analyse isole le *premier* segment — la version
est en tête, pas n'importe où : chercher `v2` au milieu du chemin le confondrait avec une
ressource nommée ainsi. Elle reconnaît ensuite la *forme* d'une version — la lettre puis des
chiffres — pour distinguer un segment de version d'un segment de ressource comme `orders` : c'est
cette reconnaissance de forme qui aiguille vers « version demandée » ou « pas de version ». La
décision, ensuite, confronte à la liste prise en charge, en comparaison insensible à la casse
puisque `V2` et `v2` désignent la même version.

Les bords se traitent par la même logique : un chemin vide ou racine n'a pas de premier segment,
donc pas de version demandée, donc version par défaut — pas d'erreur, l'absence de version est un
cas normal. Un premier segment qui n'a pas la forme d'une version — une ressource directement à la
racine — retombe aussi sur le défaut.

Le coût est linéaire dans la longueur du chemin. La transposition est la distinction
absence/erreur, omniprésente : un paramètre optionnel absent prend son défaut, mais un paramètre
*fourni et invalide* doit être signalé, jamais corrigé en douce. Le repli silencieux d'une
demande explicite mais erronée est l'un des masques de bugs les plus tenaces, parce qu'il produit
un succès apparent sur une intention trahie.
