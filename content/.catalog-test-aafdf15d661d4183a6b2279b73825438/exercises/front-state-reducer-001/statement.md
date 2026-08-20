# Réduire un état immuable à partir d'une suite d'actions

Implémentez `Submission.Reduce(string state, string actions)`.

La méthode part d'un état sérialisé, applique une suite d'actions et rend le nouvel état sérialisé.
Le point central est l'immuabilité : l'état reçu ne doit jamais être modifié, la méthode construit
et renvoie une valeur neuve.

Lecture de `state` : une suite de paires `clé=valeur` séparées par des points-virgules. Chaque paire
se découpe sur son premier signe égal. Si une même clé apparaît plusieurs fois, c'est sa dernière
valeur qui compte. Un segment sans signe égal est ignoré.

Lecture de `actions` : une suite de segments séparés par des points-virgules. Chaque segment se
découpe sur son premier deux-points en un verbe et ses arguments. Un segment sans deux-points est
ignoré. Les verbes reconnus sont exactement trois :

- `set:clé=valeur` : pose ou remplace la valeur de la clé. Si les arguments ne contiennent pas de
  signe égal, l'action est ignorée ;
- `del:clé` : retire la clé si elle est présente, sinon ne fait rien ;
- `inc:clé` : lit la valeur courante comme un entier, une clé absente valant zéro, puis l'augmente
  de un. Si la clé est présente mais que sa valeur n'est pas un entier valide, l'action est ignorée
  et rien ne change.

Tout autre verbe est ignoré silencieusement, segment par segment : aucune action mal formée ne fait
échouer l'ensemble.

Sortie : les paires restantes, clés triées en ordre ordinal croissant, jointes sous la forme
`clé=valeur` par des points-virgules. Un état vide rend la chaîne vide. Un `state` ou un `actions`
absent lève `ArgumentNullException`.

Écrivez avant le code : une pose sur état vide, un incrément sur clé absente, une suppression, et un
incrément refusé sur une valeur textuelle.

Exemple : entrée `["x=9;y=8", "del:x;set:z=7"]`, sortie `"y=8;z=7"`.
