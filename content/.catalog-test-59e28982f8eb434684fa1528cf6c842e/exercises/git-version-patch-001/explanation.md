# Explication

Incrémenter le troisième nombre d'une version : l'opération est triviale, et elle n'a de sens
que si l'on sait *pourquoi* c'est le troisième — la question que l'énoncé pose en demandant ce
qui distingue une correction d'un ajout et d'une rupture.

Le versionnage sémantique est un contrat de communication en trois nombres. Le majeur annonce
une *rupture* : du code qui consommait l'ancienne version peut casser — les consommateurs
doivent lire les notes avant de monter. Le mineur annonce un *ajout* compatible : de nouvelles
capacités, rien de retiré — monter est sûr, adopter les nouveautés est optionnel. Le patch
annonce une *correction* : le comportement promis est réparé, rien d'autre ne change — monter
devrait être un réflexe. Incrémenter le patch, c'est donc faire une promesse précise : « même
contrat, défauts en moins ». Se tromper de composant n'est pas une coquille, c'est une fausse
déclaration — un ajout étiqueté patch surprend les gestionnaires de dépendances configurés
pour prendre les corrections automatiquement ; une rupture étiquetée mineure casse des
consommateurs qui avaient confiance.

La fonction encode cette promesse en ne touchant *que* le troisième composant — les deux
premiers traversent tels quels — et en validant tout le reste : exactement trois segments,
chacun un entier non négatif. La version à deux composants, le segment textuel, le négatif —
toutes lèvent, d'un bloc, car interpréter une version illisible fabriquerait une étiquette de
publication fausse, le genre d'artefact qui pollue un dépôt de paquets pour toujours. Le refus
groupé au seuil de la fonction est l'application du « échouer tôt » aux métadonnées de
livraison.

Deux détails d'arithmétique complètent : le passage de neuf à dix — le cas caché de l'énoncé —
verrouille que l'incrément est *numérique* et non textuel : une implémentation qui
manipulerait le dernier caractère rendrait un zéro absurde. Et le `checked` couvre la borne
extrême du type — improbable, mais un compteur qui s'enroule en négatif produirait une version
qui remonte le temps.

La reconstruction par interpolation reformate depuis les entiers analysés : les zéros de tête
éventuels de l'entrée ne survivent pas, la sortie est canonique.

Le coût est constant. La transposition est le réflexe de version au moment de publier :
qu'est-ce qui a changé — rien du contrat, du contrat en plus, du contrat cassé ? — et le
composant correspondant, seul, s'incrémente. L'outillage — ici cette fonction, ailleurs la
chaîne de livraison — mécanise l'incrément ; le *choix* du composant reste un acte de
communication dont l'équipe entière dépend.
