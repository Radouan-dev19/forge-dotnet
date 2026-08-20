# Explication

La différence entre filtrer des lignes et filtrer des groupes est une des rares subtilités du
relationnel qui traverse intacte toutes les couches d'abstraction : elle existe en SQL manuscrit, elle
existe dans le traducteur de requêtes, et elle produit dans les deux cas le même genre de bogue — un
résultat plausible qui répond à une autre question que celle posée.

**Pourquoi le plancher ne peut pas se vérifier sur une ligne.** « Ce statut revient au moins deux
fois » ne se lit sur aucune commande individuelle : c'est une propriété émergente, qui n'existe
qu'après le regroupement. Le prédicat posé avant le regroupement filtre des lignes — « les commandes
dont le statut vaut telle valeur » — et le même prédicat posé après filtre des groupes — « les statuts
dont le compte atteint tel plancher ». Les deux écritures se ressemblent, compilent toutes les deux,
et divergent en silence. En SQL, la grammaire force la distinction avec deux mots-clés différents ;
dans une chaîne de requête objet, seule la **position** de l'appel de filtrage porte cette sémantique,
et c'est précisément ce qui rend l'erreur facile : déplacer une ligne de code change la question.

**Ce que le fournisseur en fait.** Le regroupement suivi d'un filtre sur le compte se traduit en
agrégation avec clause de filtrage de groupes côté serveur : le moteur regroupe, compte, élimine les
groupes sous le plancher, et ne transmet que les survivants. La projection en clé et compte borne le
résultat au nombre de statuts distincts — trois lignes au plus ici, quel que soit le nombre de
commandes. C'est le rapport de forces à retenir : le volume traité croît avec la table, le volume
transféré croît avec le nombre de groupes retenus. Rapatrier les commandes pour regrouper en mémoire
inverse ce rapport — tout le volume traverse le réseau pour finir écrasé en trois entrées.

**Pourquoi les statuts absents sont omis et non mis à zéro.** Le dictionnaire rendu alimente un
affichage : une entrée à zéro y ferait apparaître une catégorie que le plancher devait justement
taire. Plus profondément, le filtre de groupes ne produit pas des groupes vides — il les élimine. Les
faire renaître à zéro dans la matérialisation serait une seconde requête déguisée, avec sa propre
sémantique. Le dictionnaire vide, lui, est une réponse complète : aucun statut n'atteint le plancher,
et l'appelant peut l'afficher tel quel.

**Le refus du plancher non positif protège le sens du filtre.** À zéro, tout groupe passe et la
fonction devient un simple comptage par statut — une question légitime, mais différente, que
l'appelant doit poser explicitement plutôt qu'obtenir par un paramètre dégénéré.

La transposition est directe : rapports d'erreurs par code, sessions par pays, ventes par rayon —
chaque fois qu'un seuil s'applique à un agrégat, la position du filtre décide si la base élimine des
groupes ou si l'application maquille des lignes.
