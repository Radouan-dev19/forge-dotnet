# Explication

Un garde de route répond à une question en apparence simple, laisser entrer ou non, mais la bonne
réponse n'est pas binaire : elle a trois issues, et les confondre produit des failles ou des
frustrations. Laisser passer concerne un utilisateur authentifié qui possède le droit exigé.
Interdire concerne un utilisateur authentifié à qui ce droit manque : il est bien connu, mais cette
porte-là ne lui est pas ouverte. Rediriger vers la connexion concerne un utilisateur que le système
ne reconnaît pas encore, faute de jeton lisible et valide. Le piège le plus courant est de traiter
un droit manquant comme une absence d'authentification : on renvoie alors vers la connexion un
utilisateur déjà connecté, qui se reconnecte, revient, et se heurte au même mur, sans jamais
comprendre qu'aucune connexion ne lui donnera ce droit. L'inverse est pire encore : traiter un
utilisateur inconnu comme un simple manque de droit lui affiche une page interdite au lieu de lui
offrir de s'identifier.

Le cœur technique est le décodage, et c'est là que les cas cachés frappent. Un JWT est fait de trois
segments séparés par des points ; le segment du milieu porte les revendications, encodé en base64url.
Le base64url n'est pas du base64 ordinaire : deux caractères diffèrent et le remplissage de fin est
retiré. Décoder sans d'abord rétablir cette forme échoue sur certains jetons de façon intermittente,
selon que leur contenu produit ou non les caractères concernés. Chaque étape du décodage peut
échouer, et toutes ces défaillances doivent converger vers la même issue : la redirection. Une chaîne
qui n'est pas un jeton, un segment illisible, un JSON invalide, une revendication d'expiration
absente, tout cela décrit un porteur qu'on ne peut pas authentifier, et non un accès à interdire.
Laisser une exception de décodage remonter transformerait une entrée hostile ordinaire en erreur
serveur, ce qui est à la fois un défaut de robustesse et une fuite d'information.

L'expiration se lit sur le jeton lui-même, et la borne compte : un jeton dont l'instant d'expiration
est exactement l'instant courant est considéré expiré. Un cas caché vise précisément cette égalité.
Décider que la validité s'arrête à l'instant nommé, plutôt qu'une seconde après, évite une fenêtre où
un jeton juste échu resterait accepté.

La comparaison des droits doit être exacte, et c'est le sens d'un autre cas caché. Les droits sont
une liste séparée par des espaces ; le droit exigé doit y figurer à l'identique. Une comparaison par
préfixe laisserait un droit nommé `orders.readonly` satisfaire une exigence `orders.read`, ouvrant
une porte sur la foi d'une ressemblance de texte. La sécurité se construit sur des correspondances
exactes, jamais sur des débuts communs.

Un point de méthode, enfin. Ce garde ne vérifie pas la signature, et l'énoncé le dit explicitement.
La confiance dans le contenu du jeton repose entièrement sur une validation cryptographique faite en
amont. Lire des revendications sans cette étape reviendrait à croire n'importe qui sur parole ; le
garde suppose cette étape faite et se concentre sur la décision d'accès. Le coût est linéaire dans la
taille du jeton. La transposition vaut pour tout contrôle d'accès porté par une revendication signée,
d'une passerelle d'API à un intergiciel de session.
