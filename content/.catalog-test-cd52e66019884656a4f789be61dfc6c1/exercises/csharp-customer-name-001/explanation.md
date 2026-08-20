# Explication

Trois mots dans cette solution portent chacun une décision, et l'exercice existe pour qu'on les
lise comme telles plutôt que comme du vocabulaire appris par cœur.

`IsNullOrWhiteSpace` d'abord, et sa position en tête. Un nom absent, vide ou composé d'espaces
n'a rien à normaliser : toute opération dessus est soit une exception de référence nulle, soit
la fabrication d'une chaîne vide déguisée en nom. La garde regroupe ces trois états en un seul
cas et rend une valeur de repli nommée — `(inconnu)` — que l'affichage peut montrer telle
quelle. Le choix du repli plutôt que de l'exception est un contrat d'affichage : la fonction
prépare une donnée pour un écran ou un rapport, où une ligne « inconnu » vaut mieux qu'un
traitement interrompu. Dans un parcours d'enregistrement, la décision inverse serait la bonne.
L'ordre compte autant que la garde elle-même : tester *avant* de déréférencer est ce qui rend le
`Trim()` de la ligne suivante sûr.

`Trim` ensuite. Les espaces de bord sont le bruit de saisie le plus universel — copier-coller,
champ de formulaire, export tableur — et les retirer avant toute comparaison ou stockage évite
la classe entière des « doublons invisibles » : deux clients identiques à un espace près. Le
milieu de la chaîne n'est pas touché : un nom composé garde son espace intérieur.

`ToUpperInvariant` enfin, et surtout son suffixe. Mettre en majuscules *dépend de la culture* :
la casse du i sans point turc est l'exemple canonique où `ToUpper` sous une culture donnée
produit un caractère différent de celui attendu par le reste du système. `Invariant` fige la
règle indépendamment de la machine qui exécute — deux serveurs configurés différemment
normalisent pareil, un test passe partout. Pour une clé de comparaison ou un identifiant
d'affichage stable, c'est le seul choix défendable ; la casse culturelle se réserve au texte
montré à un humain dans sa langue.

Les cas cachés balaient les états de la garde — `null`, chaîne vide, espaces seuls — et un nom
déjà propre, qui doit traverser sans dommage ; le nominal à espaces de bord vérifie l'ordre
rognage-puis-casse. Le coût est linéaire avec deux allocations au plus, sans enjeu.

La transposition est le trio lui-même : garde d'absence, nettoyage de bords, normalisation
invariante — dans cet ordre — est le gabarit de toute entrée textuelle qui devient clé, code ou
libellé stable. Chaque fois qu'une comparaison de chaînes échoue « parfois », l'un des trois
manque, et c'est presque toujours l'invariant.
