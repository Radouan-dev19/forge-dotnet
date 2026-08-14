# Explication

Une expiration avec délai de grâce est un problème d'une ligne qui fait échouer des systèmes
réels, parce que tout s'y joue sur une frontière que chacun place ailleurs : le dernier jour de
grâce est-il encore valide ? L'énoncé tranche — oui, et l'expiration commence *strictement
après* — et la solution n'a plus qu'à transcrire cette phrase sans la déformer.

La transcription passe par une variable intermédiaire, et c'est un choix de lisibilité qui
mérite sa défense. `dueDate.AddDays(graceDays)` est *la dernière date valide* : lui donner ce
nom transforme la comparaison finale en lecture du contrat — `today > lastValidDate` se lit « on
est après la dernière date valide, donc expiré ». La version condensée en une expression rend le
même booléen mais oblige chaque relecteur à refaire mentalement la dérivation ; sur du code de
règle métier, nommer l'étape intermédiaire est presque toujours le bon échange. L'alternative
mathématiquement équivalente — comparer l'écart de jours au délai de grâce — recalcule ce que
`DateOnly` sait déjà faire et multiplie les occasions d'inverser un signe.

La comparaison stricte est le point que les cas cachés attaquent des deux côtés : *le jour
même* de la dernière date valide, l'élément n'est pas expiré ; le lendemain, il l'est. Écrire
`>=` déplace la frontière d'un jour entier, et ce genre d'erreur ne se voit jamais en
développement — elle se voit le jour où un client perd un accès la veille de ce que le contrat
commercial lui promettait. Une grâce de zéro jour est aussi un cas de bord parlant : la dernière
date valide est l'échéance elle-même, et la fonction doit rester juste sans branche spéciale.

Le refus de la grâce négative relève du même principe que partout ailleurs dans le catalogue :
une durée négative ne décrit rien dans ce domaine — ce serait une expiration anticipée déguisée —
et inventer un comportement plutôt que lever `ArgumentOutOfRangeException` masquerait un bug de
l'appelant. La validation vient en tête, avant tout calcul.

Un mot sur ce que la signature enseigne en creux : `today` est un *paramètre*. La fonction ne
consulte pas l'horloge système — elle serait alors intestable et changerait de résultat à
minuit. Recevoir la date courante de l'extérieur est la technique qui rend les règles
temporelles déterministes, et c'est probablement la transposition la plus rentable de tout
l'exercice : chaque fois qu'un `DateTime.Now` apparaît au milieu d'une règle métier, il faut le
remonter en paramètre. Le coût d'exécution, lui, est constant et sans histoire — toute la
valeur est dans la frontière.
