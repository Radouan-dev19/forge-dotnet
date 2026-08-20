# Explication

Une validation qui s'arrête au premier défaut paraît économe et coûte cher. Un client dont trois
champs sont fautifs corrige le premier, renvoie, découvre le deuxième, corrige, renvoie, découvre le
troisième. Trois allers-retours pour une information que le serveur détenait entièrement dès le
premier appel. C'est la raison d'être du dictionnaire de violations d'une réponse d'erreur
normalisée : il porte tout ce qui bloque, en une fois.

Le deuxième point est moins visible et tout aussi structurant : l'ordre du rapport. Le faire suivre
l'ordre du corps reçu semble naturel, puisque c'est l'ordre de lecture. Cela rend pourtant deux
appels équivalents incomparables : le même défaut sur les mêmes champs produit deux rapports
différents selon la façon dont le client a rangé sa demande. Un test doit alors trier avant de
comparer, ou pire, accepter n'importe quel ordre — et cesse par là de détecter qu'un champ a
disparu du rapport. Parcourir les champs attendus dans leur ordre de déclaration supprime le
problème à la source, et c'est aussi ce qui permet de parler d'une réponse stable dans un contrat.

Le champ répété mérite sa propre catégorie. La tentation est de garder la dernière valeur, ce que
font beaucoup de liaisons de modèle. Mais choisir silencieusement laquelle des deux valeurs fait foi,
c'est décider à la place du client d'une chose qu'il n'a pas dite. Le signaler comme défaut de forme
et ne pas contrôler la valeur est le seul comportement qui ne cache rien : il n'y a pas de valeur à
contrôler tant qu'on ne sait pas laquelle est la bonne.

Le nom inattendu, lui, se range après les champs attendus et garde l'ordre du corps. Deux raisons.
Un nom inconnu n'a pas d'ordre de déclaration, puisqu'il n'est déclaré nulle part ; le seul ordre
disponible est celui de réception. Et le placer après conserve en tête du rapport ce que le client
doit corriger en priorité — un champ obligatoire absent bloque la demande, une clé en trop est
souvent une faute de frappe ou une version d'API plus récente.

Le découpage sur le **premier** signe égal seulement n'est pas un détail. Une valeur peut en contenir
un autre — une chaîne encodée, un jeton, un filtre. Découper sur tous les signes égaux ferait
disparaître une partie de la valeur, et le champ serait déclaré invalide pour une raison que le
client ne pourrait pas comprendre depuis le rapport.

Le coût est le produit du nombre de segments par le nombre de champs attendus. Ce dernier est fixe et
petit ; le parcours reste donc linéaire en pratique. Un dictionnaire des occurrences ferait mieux en
théorie et n'apporterait rien ici, où les corps comptent quelques champs.
