# Explication

Le titre de l'exercice dit son vrai sujet : « sans exposer la mutation ». Le nettoyage — ramener
les négatifs à zéro — n'est qu'un prétexte ; ce qui est enseigné, c'est la différence entre
transformer *pour soi* et transformer *chez l'appelant*.

Un tableau, en C#, est un objet partagé : le paramètre `values` n'est pas une copie des données,
c'est une référence vers les données de l'appelant. Écrire `values[i] = ...` modifie donc son
tableau à lui, et cette modification survit au retour de la méthode. La version fautive qui
nettoie en place puis retourne `values` rend exactement les bonnes valeurs — tous les tests qui
ne regardent que la sortie passent — et laisse derrière elle un appelant dont les données ont
changé sans son accord. C'est le genre de bug qui se manifeste loin de sa cause : un autre bout
de code relit le tableau plus tard et trouve des zéros là où il avait mis des négatifs. Le
harnais de cet exercice ferme la porte : des cas dédiés capturent l'argument avant l'appel et le
comparent après, si bien que la mutation cachée échoue même quand la valeur de retour est
parfaite.

La solution alloue donc une collection neuve de même taille et n'écrit que dans elle.
`Math.Max(0, values[i])` fait le nettoyage par un plancher : les négatifs remontent à zéro, zéro
et les positifs passent inchangés — un cas caché mélange les trois pour vérifier qu'aucune
condition trop zélée ne touche aux valeurs licites. L'allocation systématique, même quand aucune
valeur n'est négative, est un choix délibéré : retourner l'original « parce qu'il n'y avait rien
à nettoyer » créerait deux régimes de propriété — parfois l'appelant reçoit du neuf, parfois son
propre tableau — et ce genre d'inconstance finit toujours par être exploité par erreur.

Le `null` reste une faute d'appel, distincte du tableau vide qui, lui, rend un tableau vide neuf
par le jeu naturel de l'allocation et de la boucle.

Le coût est linéaire en temps et en espace, incompressible dès lors qu'on promet une copie. La
transposition est un réflexe d'interface : toute méthode qui reçoit une collection et rend une
collection « corrigée » doit choisir entre muter et copier, l'écrire dans son nom et son
contrat, et s'y tenir. Les ennuis ne viennent jamais du choix — les deux se défendent — mais du
non-dit. Ici, `SanitizeCopy` porte le choix dans son nom ; c'est la bonne habitude à emporter.
