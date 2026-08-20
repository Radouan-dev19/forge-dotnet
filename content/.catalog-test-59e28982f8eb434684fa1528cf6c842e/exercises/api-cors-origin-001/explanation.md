# Explication

Résoudre l'origine autorisée est une décision de trois lignes dont chacune ferme une faille, et
la plus subtile est l'exclusion du joker quand la requête porte des identifiants — un verrou de
la spécification qu'on désactive sans le vouloir en écrivant du code « permissif ».

Le joker n'est légitime que dans un seul cas : une ressource *vraiment* publique — liste ouverte
à tous — et une requête *sans* identifiants. Là, `*` dit « n'importe qui peut lire cette réponse
anonyme », sans danger, puisqu'il n'y a pas de session de l'utilisateur à exposer. Dès que des
identifiants entrent en jeu — un cookie, un jeton —, la spécification interdit d'associer le joker,
et les navigateurs *bloquent* la réponse qui le tenterait. La raison est directe : autoriser
« toute origine » *et* « avec les cookies de l'utilisateur » rouvrirait exactement ce que la
politique de même origine ferme — n'importe quel site lirait les réponses authentifiées de la
victime. L'exclusion n'est pas une chicane, c'est le verrou qui empêche de désactiver la
protection par mégarde, et le cas caché « joker avec identifiants » vérifie qu'on ne le force pas.

Hors du cas joker, la seule origine autorisable est une origine *nommée et listée*. La méthode
renvoie alors l'origine reçue — mais seulement *après* l'avoir confrontée à la liste. C'est la
nuance qui sépare l'écho sûr de l'écho dangereux : renvoyer une origine validée est correct,
renvoyer l'origine reçue *sans* la valider est le joker interdit déguisé — le serveur dirait « oui,
toi précisément » à chaque site, identifiants compris, contournant le verrou. L'écho aveugle est
l'erreur la plus grave du domaine parce qu'elle *ressemble* à une liste blanche tout en n'en étant
pas une. Le cas caché de l'origine hors liste vérifie qu'elle rend une autorisation vide, pas son
propre écho.

L'analyse de la liste — découpage, rognage, ensemble — est le vocabulaire habituel des listes
encodées, et la comparaison est exacte : une origine est un triplet schéma-hôte-port, et toute
tolérance élargirait l'ensemble atteignable par un attaquant qui contrôle un sous-domaine ou un
port voisin.

Le coût est linéaire dans la taille de la liste. La transposition est le principe de la liste
blanche appliqué aux origines, et sa règle d'or : ne jamais renvoyer au demandeur une valeur
dérivée de sa propre requête sans l'avoir confrontée à un ensemble fermé décidé par le serveur.
L'écho contrôlé est sûr ; l'écho aveugle est une porte ouverte, en CORS comme dans les
redirections ou les listes de tri.
