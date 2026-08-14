# Explication

Le paramètre de tri d'une API a une particularité qui le rend dangereux : il finit, d'une
manière ou d'une autre, dans une *requête*. L'énoncé demande ce qu'une clé concaténée sans
liste fermée permettrait d'injecter, et la réponse est le nom de la faille la plus ancienne du
métier : si la valeur du client rejoint le texte d'une commande — SQL, tri dynamique, expression
compilée —, le client écrit dans votre requête. Même quand la construction est paramétrée, un
nom de colonne ne se paramètre pas comme une valeur : la seule défense est de ne jamais laisser
passer un nom que le serveur n'a pas *lui-même* écrit.

D'où la liste blanche, et sa mécanique en deux temps. La normalisation d'abord : l'absence est
absorbée par l'opérateur de coalescence — un paramètre non fourni devient chaîne vide, candidate
comme une autre —, puis bords rognés et minuscules invariantes, le traitement standard des
identifiants techniques, pour que `Date` et `date` désignent la même clé publique. La
confrontation ensuite : trois clés autorisées, énumérées dans le code — `date`, `total`,
`status` — et *tout le reste* retombe sur `id`, le tri par défaut. Le domaine de sortie de
cette fonction est fini et connu : quatre valeurs possibles, toutes écrites par le serveur.
C'est la propriété qui compte — quoi que le client envoie, ce qui atteint la requête vient du
code, jamais du réseau.

Le choix du *repli silencieux* — défaut plutôt qu'erreur — mérite sa discussion, comme pour la
taille de page voisine : une clé inconnue est le plus souvent une faute de frappe ou une
version d'interface en avance, et servir la liste triée par défaut est plus hospitalier qu'un
rejet. Le prix est le même qu'au bornage : le client doit pouvoir *voir* le tri réellement
appliqué — sans quoi il croira sa clé honorée. L'alternative stricte — refuser la clé
inconnue — se défend pour des API à contrat ferme ; ce qui ne se défend jamais, c'est la
concaténation confiante.

Notons aussi ce que la liste blanche *découple* : les clés publiques — le vocabulaire du
contrat — ne sont pas les noms de colonnes — le vocabulaire du schéma. La table de
correspondance clé-vers-colonne vit plus loin, côté requête, et ce découplage permet de
renommer une colonne sans casser l'API.

Les cas suivent l'énoncé : la clé autorisée qui passe, la casse qui converge, l'inconnue et
l'absente qui retombent sur `id`.

Le coût est constant. La transposition couvre tout paramètre qui désigne une *structure* plutôt
qu'une valeur : champs de tri, colonnes de projection, directions, noms de filtres — liste
fermée, normalisation, repli décidé. La règle en une phrase : une valeur se paramètre, un nom
se choisit dans une liste.
