# Explication

Traduire un code numérique en libellé tient en une expression `switch`, et l'exercice se
concentre sur la seule branche qui demande du jugement : celle qu'on n'a pas prévue.

L'expression `switch` avec ses bras `0 => "draft", 1 => "paid", 2 => "shipped"` est une table de
correspondance rendue lisible : une entrée, une sortie, aucune logique cachée. Sa supériorité
sur la cascade de `if` n'est pas seulement esthétique — le compilateur *exige* l'exhaustivité.
Sans le bras `_`, le code ne compile pas, parce qu'un `int` peut porter bien d'autres valeurs
que trois. Cette exigence force à répondre à la question que les cascades de `if` permettent
d'esquiver : que fait-on d'un statut trois, ou de moins un ?

La réponse du contrat est la partie instructive : rendre `unknown`, un état *nommé* qui dit la
vérité. Les alternatives se jugent par leurs conséquences. Rendre un libellé valide par défaut —
`draft`, disons — fabrique un mensonge : une commande au statut corrompu s'afficherait comme
brouillon, et personne ne chercherait jamais le bug, puisque rien ne semble cassé. Lever une
exception est défendable dans un pipeline de traitement, où un statut inconnu doit arrêter la
chaîne ; pour une fonction d'affichage, l'exception transforme une ligne de données abîmée en
écran d'erreur entier. `unknown` occupe le milieu juste : le tableau de bord montre la commande,
le libellé signale l'anomalie, et une recherche sur ce libellé retrouve toutes les lignes à
investiguer. Le choix entre ces trois régimes — valeur sentinelle, exception, silence — revient
dans chaque traduction de données externes, et savoir le poser explicitement est la compétence
visée.

Les cas cachés éprouvent la frontière des deux côtés : les valeurs juste au-delà des codes
connus — trois, moins un — doivent rendre `unknown`, et chaque code connu son libellé exact.
C'est aussi une protection de non-régression : le jour où un statut quatre entre au catalogue,
le test qui attend `unknown` pour quatre échouera, forçant la mise à jour *consciente* de la
table et de ses consommateurs — plutôt qu'une valeur nouvelle silencieusement affichée comme
inconnue en production pendant des semaines.

Le coût est constant, la table étant compilée en comparaison directe. La transposition va
au-delà des statuts : codes de retour d'API partenaires, types d'événements, catégories de
fichiers — toute donnée énumérée venue de l'extérieur doit traverser une table exhaustive dont
le cas par défaut est un état honnête, jamais une valeur plausible. « Rendre l'inconnu
explicite », comme dit l'énoncé, est une règle de sûreté déguisée en détail d'affichage.
