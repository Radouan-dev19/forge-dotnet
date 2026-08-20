# Explication

Cet exercice fait faire à la main ce que toute bibliothèque de jetons fait avant la moindre
vérification : retrouver du JSON lisible à partir d'un segment encodé. Le faire soi-même une fois
change la façon dont on lit ensuite la documentation d'un middleware — on sait ce qui se passe
sous chaque option.

Le premier choix structurant est l'ordre des refus. Le découpage en segments vient avant tout le
reste : tant que le jeton ne porte pas trois parties, parler de charge utile n'a pas de sens, et
c'est pourquoi la méthode lève dès ce stade. Le décodage vient ensuite, et il concentre la vraie
difficulté du sujet : l'alphabet Base64Url n'est pas celui que `Convert.FromBase64String` attend.
Deux caractères diffèrent — le tiret remplace le plus, le souligné remplace la barre oblique — et
le remplissage final a été supprimé à l'émission. Il faut donc restaurer les deux. Le calcul du
remplissage mérite d'être compris plutôt que recopié : un flux Base64 encode trois octets en quatre
caractères, si bien qu'une longueur valide modulo quatre vaut zéro, deux ou trois. Un reste de deux
appelle deux signes égal, un reste de trois en appelle un seul, et un reste de un est
mathématiquement impossible — le rencontrer prouve que le segment a été tronqué en chemin, et la
seule réponse honnête est une exception de format, pas une tentative de réparation.

Le deuxième choix est la distinction entre trois issues que les débutants confondent : le jeton
malformé, la revendication absente et la revendication présente. Le premier est une erreur de
l'entrée, signalée par `ArgumentException` ; l'appelant ne peut rien en faire d'autre que rejeter.
La deuxième est une situation normale — toutes les charges utiles ne portent pas toutes les
revendications — et se signale par une valeur neutre, la chaîne vide, que l'appelant peut tester.
Mélanger les deux, par exemple en levant aussi pour une revendication absente, rendrait la méthode
inutilisable pour sonder un jeton.

Le troisième choix concerne le type de la valeur. Une revendication JSON peut être une chaîne, un
nombre, un tableau. Retourner `GetRawText()` sur une chaîne rendrait ses guillemets, ce qui
surprendrait tout appelant ; retourner `GetString()` sur un nombre lèverait. La règle retenue —
valeur pour une chaîne, texte brut pour le reste — est celle qui préserve l'information sans
surprise. Enfin, rappelez-vous ce que cet exercice ne fait pas : aucun contrôle de signature n'a
eu lieu, donc rien de ce qui est lu ici n'est digne de confiance. Décoder sert à router ou à
diagnostiquer, jamais à décider — la décision exige la chaîne de validation complète, objet des
exercices suivants.
