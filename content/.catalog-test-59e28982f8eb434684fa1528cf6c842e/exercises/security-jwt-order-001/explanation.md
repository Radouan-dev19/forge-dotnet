# Explication

Cet exercice assemble les briques des quatre précédents en une chaîne, et c'est la chaîne
elle-même — pas les briques — qui est évaluée. Le verdict retourné nomme le premier contrôle en
échec : une implémentation qui vérifie tout mais dans le désordre rend des verdicts faux, et les
cas cachés sont construits pour la démasquer.

Pourquoi cet ordre-là ? La ligne de partage passe entre ce qui s'établit sans faire confiance au
contenu et ce qui exige cette confiance. La forme, l'algorithme et la signature appartiennent à la
première famille : la forme se constate, l'algorithme se confronte à une décision du serveur, la
signature se recalcule avec la clé du serveur. Les revendications appartiennent à la seconde : une
date d'expiration, un émetteur, une audience n'ont de valeur probante qu'une fois prouvé que
personne ne les a réécrits. Rendre un verdict d'expiration sur un jeton à la signature fausse,
c'est répondre à l'attaquant sur la base du contenu qu'il a lui-même choisi — et lui confirmer au
passage que sa charge utile est lue, ce qui l'aide à calibrer la suite. Le cas caché le plus
important de la suite éprouve exactement cela : un jeton expiré *et* falsifié doit rendre le
verdict de signature, jamais celui d'expiration.

À l'intérieur de chaque famille, l'ordre reste contractuel. Entre émetteur et audience, aucune
nécessité cryptographique n'impose un premier ; mais un validateur dont le verdict dépend de
l'humeur d'implémentation est intestable, et l'appelant — un middleware, un journal d'audit — a
besoin de savoir ce que signifie chaque verdict. Fixer l'ordre dans le contrat transforme une
ambiguïté en spécification, et l'exercice montre qu'une spécification de ce genre se teste : le
jeton dont l'émetteur et l'audience sont tous deux faux a un verdict déterminé, pas deux verdicts
possibles.

Deux détails de construction méritent attention. D'abord, la portée du contrôle de format : il
couvre les trois segments, y compris le décodage de la signature, avant tout autre verdict. Un
en-tête illisible est un problème de format, pas d'algorithme — le verdict d'algorithme affirme
qu'une annonce lisible diffère de l'exigence, ce qui est une autre information. Ensuite,
l'expiration est ici stricte et sans tolérance, contrairement à l'exercice de fenêtre de validité :
la tolérance est une politique du déploiement, pas une propriété du jeton, et ce validateur-ci
reçoit l'instant courant en paramètre précisément pour que la politique reste au-dehors. Comparer
les deux exercices montre où passe la frontière entre le mécanisme et sa configuration.

Enfin, la méthode ne lève jamais : chaque anomalie a un verdict nommé. C'est ce qui la rend
composable — un middleware peut journaliser le verdict, le compter, le traduire en réponse HTTP —
et c'est la différence entre un vérificateur et un simple décodeur.
