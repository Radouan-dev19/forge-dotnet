# Explication

Le préflight est une demande de permission préalable : le navigateur annonce ce qu'il *voudrait*
faire — une méthode et un ensemble d'en-têtes — et n'envoie la vraie requête que si le serveur
confirme *les deux*. Cet exercice implémente cette confirmation, et sa leçon centrale est la
conjonction : la méthode autorisée ne suffit pas, il faut aussi que chaque en-tête le soit.

La vérification a deux volets indépendants. La méthode demandée doit appartenir à l'ensemble
autorisé — un `PUT` refusé arrête tout. Et *chaque* en-tête demandé doit figurer dans l'ensemble
autorisé : c'est un ET logique sur toute la liste, et un seul en-tête manquant suffit à refuser
le préflight entier. L'erreur classique valide sur la seule méthode — « le PUT est autorisé, donc
c'est bon » — en oubliant qu'un en-tête personnalisé non déclaré, comme un jeton dans un en-tête
propriétaire, doit lui aussi avoir été confirmé. Le cas caché à l'en-tête non autorisé débusque
précisément ce raccourci : méthode permise, un en-tête de trop, préflight refusé.

Le sens de l'inclusion mérite attention, car il se confond facilement : ce sont les *demandés*
qui doivent être *autorisés*, pas l'inverse. La liste autorisée peut être plus large — le serveur
déclare tout ce qu'il accepte —, et le préflight passe tant que la demande y est incluse. Vérifier
l'inclusion dans le mauvais sens — exiger que tous les en-têtes autorisés soient demandés —
refuserait des requêtes parfaitement légitimes qui n'utilisent qu'une partie de ce que le serveur
permet.

La liste d'en-têtes demandés *vide* n'est pas un refus : c'est une absence d'exigence. Une requête
qui ne demande aucun en-tête particulier n'a rien à faire confirmer de ce côté, et le préflight se
décide alors sur la seule méthode. Traiter le vide comme un refus casserait les requêtes les plus
simples, et le cas caché « aucun en-tête demandé » vérifie ce comportement.

Les comparaisons sont insensibles à la casse des deux côtés : les noms de méthodes et d'en-têtes
HTTP le sont par définition — `Content-Type` vaut `content-type` —, et une comparaison stricte
refuserait des demandes conformes sur une simple différence de casse.

Le coût est linéaire dans la taille cumulée des listes. La transposition est le contrôle
d'inclusion d'un ensemble demandé dans un ensemble permis, avec la bonne direction et la bonne
gestion du vide : c'est le squelette de toute validation de capacités négociées — portées
demandées contre portées accordées, formats demandés contre formats servis. La conjonction
« tout le demandé doit être permis » et le refus au premier manquant sont le cœur réutilisable.
