# Explication

La pagination paraît un problème d'interface — découper une liste en écrans — alors que c'est un
problème de base de données : comment reprendre un parcours au milieu d'une table qui vit. Les deux
familles de réponses, décalage et jeu de clés, se distinguent sur les deux axes qui comptent en
production : la stabilité sous écritures concurrentes et le coût d'accès aux pages profondes.

**Le décalage recompte, le curseur repère.** Sauter cinquante lignes puis en prendre dix définit la
page par sa **position** : toute insertion ou suppression en amont pendant le parcours décale la
numérotation, et le lecteur voit la dernière ligne de sa page précédente réapparaître — ou une ligne
disparaître sans avoir jamais été affichée. Le curseur définit la page par son **contenu** : « les
lignes après cet identifiant » reste vrai quelles que soient les écritures ailleurs dans la table. La
même propriété paie sur le coût : le décalage force le moteur à produire puis jeter toutes les lignes
sautées, un prix qui croît avec la profondeur ; le curseur se résout par une recherche dans l'index de
la clé, au même prix pour la page mille que pour la première.

**La comparaison stricte est la moitié du contrat.** Le curseur est le dernier identifiant **déjà
lu** ; une comparaison large le servirait deux fois, une ligne dupliquée à chaque frontière de page.
L'erreur est discrète — elle ne se voit que sur les lignes de jonction — et systématique. La
comparaison stricte a une conséquence élégante : un curseur qui ne correspond plus à aucune ligne —
l'enregistrement a été supprimé entre deux pages — fonctionne sans traitement particulier, puisque la
question « ce qui est après » n'exige pas que le point de repère existe encore.

**L'ordre appartient à la requête, avant la limitation.** Sans clause d'ordre, une base relationnelle
ne promet aucun ordre : les lignes sortent selon le plan d'exécution du moment. Limiter d'abord, c'est
tronquer cet ordre accidentel — le résultat peut changer d'une exécution à l'autre sans qu'aucune
donnée n'ait bougé. La séquence filtre, ordre, limite, écrite dans la requête, est la seule qui donne
au mot « page » un sens stable ; et l'insertion volontairement désordonnée du jeu de données de cet
exercice existe précisément pour punir l'ordre implicite.

**La page vide est un signal, pas un échec.** Le parcours se termine quand la requête ne rend plus
rien ; refuser ce cas obligerait l'appelant à connaître la fin avant de la demander, ce qui est
contradictoire. Les bornes de la taille de page, elles, sont bien des refus : une page de zéro ne
progresse pas, une page démesurée est un contournement de la pagination qui rapatrie la table sous un
autre nom.

La transposition : toute interface de liste — journaux, transactions, messages — finit par rencontrer
les doublons du décalage ; le réflexe professionnel est de proposer le curseur avant que la table
grossisse, parce que le changement d'interface coûte bien plus cher après.
