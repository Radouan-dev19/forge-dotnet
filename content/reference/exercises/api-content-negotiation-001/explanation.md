# Explication

La négociation de contenu est la partie la plus subtile du contrat entre un client et une API :
deux listes — ce que le client sait lire, ce que le serveur sait produire — et une règle de
rencontre. L'exercice en implémente le noyau décidable, et ses quatre décisions de conception
méritent chacune leur défense.

La première est la hiérarchie des volontés : *la qualité du client classe, l'ordre du serveur
départage*. Une implémentation où l'ordre d'écriture du client l'emporterait rendrait la réponse
dépendante d'un détail sans signification — l'ordre des entrées d'un en-tête n'est pas une
préférence, le facteur de qualité l'est. À l'inverse, à qualités égales, quelqu'un doit
trancher, et c'est le serveur : il connaît le coût de production de chaque format. La boucle de
la solution encode cette hiérarchie par sa structure même — elle parcourt les types *du
serveur* dans l'ordre, et la comparaison strictement supérieure fait que le premier arrivé
garde la main à égalité. Un seul caractère — `>` contre `>=` — porte toute la règle de
départage.

La deuxième est le refus explicite : une qualité de zéro n'est pas une absence d'avis, c'est un
veto. La fonction `QualityOf` le matérialise par son ordre de recherche — l'entrée exacte
d'abord, le passe-partout seulement en repli — si bien qu'un type noté zéro reste écarté même
quand `*/*` autoriserait tout. Confondre « je n'en parle pas » et « je n'en veux pas » est
l'erreur classique des implémentations naïves, et le cas caché qui couple veto et passe-partout
la débusque.

La troisième est le partage des responsabilités : aucune correspondance rend la chaîne vide —
le *serveur* en tirera son refus, avec le statut approprié. La fonction décide de la rencontre,
pas de la réponse HTTP ; garder cette frontière nette est ce qui la rend testable par table.

La quatrième est lexicale : les types se comparent sans casse — la norme des types de média —
mais le résultat rend la graphie *du serveur*, forme canonique de sortie. Et l'analyse du
facteur `q=` se fait en culture invariante : un en-tête est un protocole, pas du texte
localisé.

Le coût est le produit des deux listes — quelques entrées chacune, sans enjeu — et la structure
en petites fonctions nommées est le vrai modèle : analyse, qualité, sélection, chacune
racontable séparément.

La transposition dépasse HTTP : versions d'API acceptées contre versions servies, langues,
encodages, algorithmes de compression — toute rencontre entre capacités d'un client et d'un
serveur repose ces quatre questions. Qui classe, qui départage, comment dit-on non, et quelle
forme sort. Les réponses écrites font un protocole ; les réponses devinées font des tickets.
