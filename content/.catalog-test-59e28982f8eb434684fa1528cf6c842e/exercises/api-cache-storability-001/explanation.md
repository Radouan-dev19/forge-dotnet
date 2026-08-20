# Explication

Avant de décider *comment* mettre une réponse en cache, il faut décider *si* on le peut — et cette
question précède la directive. La stockabilité repose sur deux conditions cumulatives, la méthode
et le statut, et l'erreur classique est de n'en regarder qu'une.

La condition de méthode est la plus importante et la plus oubliée. Seules les lectures sans effet
— `GET`, `HEAD` — sont stockables, parce que resservir leur réponse depuis un cache est inoffensif :
la ressource n'a pas changé du fait de la lecture. Une méthode à effet est tout autre chose :
garder la réponse d'un `POST` qui crée une commande, puis la resservir, laisserait croire qu'une
création a eu lieu alors qu'aucune requête n'est partie — le cache mentirait sur un état du monde.
C'est pourquoi toute méthode qui change l'état, ou toute méthode inconnue, rend faux sans même
regarder le statut. Juger la stockabilité sur le seul statut — « c'est un 200, donc cacheable » —
est le piège que le cas caché de l'écriture réussie débusque : un `POST` qui répond 200 n'est
pas stockable pour autant.

La condition de statut restreint ensuite, sur les seules lectures. La liste des statuts stockables
n'est pas intuitive : on y trouve les succès classiques, mais aussi des statuts d'*absence* — le
404 introuvable, le 410 disparu — car un cache peut légitimement mémoriser « cette ressource
n'existe pas » pour éviter de reposer la question, et une redirection permanente, dont la
permanence même invite à la garder. En sont exclus les erreurs de serveur — transitoires, il
serait faux de les figer — et les réponses qui dépendent d'un état de requête. La présomption est
restrictive : hors de la liste, non — un statut qu'on ne sait pas classer n'est pas mis en cache,
même défaut prudent que la directive de cache voisine.

La normalisation de la méthode — rognage, majuscules invariantes — traite le verbe HTTP comme
l'identifiant technique qu'il est : `get` vaut `GET`, et l'invariant garantit le même verdict sur
toute machine.

Le coût est constant. La transposition est le raisonnement à deux facteurs de la mise en cache :
une réponse n'est stockable que si l'*opération* le permet — pas d'effet de bord à masquer — et
si le *résultat* le permet — un état stable et resservable. Beaucoup d'incidents de cache
viennent d'avoir regardé le second sans le premier : un cache qui garde des réponses d'actions,
ou qui fige des erreurs transitoires, transforme un accélérateur en source de comportements
fantômes. La règle tient en une phrase : ne garder que ce qu'il est sûr de resservir sans avoir
rien fait.
