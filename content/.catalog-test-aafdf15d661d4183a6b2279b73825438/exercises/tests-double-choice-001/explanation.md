# Explication

Stub, fake ou spy : choisir son double de test est une décision de conception que la plupart
des équipes prennent par habitude d'outillage — tout simuler avec la même bibliothèque, tout
vérifier par des attentes d'appels. L'exercice force à choisir selon le *besoin*, et l'arbre à
deux questions encode une doctrine qu'il faut savoir défendre.

Les trois réponses d'abord. Le *stub* rend des réponses préparées, rien de plus : c'est le
double du besoin minimal, quand le test a seulement besoin que la dépendance réponde quelque
chose pour que le sujet avance. Le *fake* porte un comportement réel simplifié — un dépôt en
mémoire, une horloge pilotable : il sert quand le scénario traverse la dépendance plusieurs
fois et que la cohérence entre les appels compte — ce qu'un stub figé ne sait pas offrir. Le
*spy* enregistre les appels reçus pour qu'on les inspecte : il ne sert que lorsque
l'*interaction elle-même* est le comportement à vérifier — un message publié, une notification
émise — parce qu'aucune sortie observable n'en témoigne autrement.

L'ordre des questions ensuite, et sa raison. L'interaction prime : si le test doit prouver
qu'un appel a eu lieu, il faut un spy, quel que soit le reste. Mais la doctrine est dans la
question que l'énoncé pose — qu'ajoute une vérification d'appel quand le résultat est déjà
vérifié ? Réponse : du couplage, pas de la confiance. Un test qui vérifie le résultat *et* les
appels internes casse à chaque refactorisation qui réorganise les appels sans changer le
comportement — il teste l'implémentation, plus le contrat. La hiérarchie saine se lit dans
l'arbre : vérifier l'état ou le résultat quand c'est possible — stub ou fake —, et réserver
l'observation des interactions aux effets qui n'ont pas d'autre témoin. Le spy est un dernier
recours codifié, pas un réflexe.

Le domaine d'entrée est fini — quatre combinaisons, que l'énoncé fait écrire — et la
combinaison double-vrai est le cas de doctrine : interaction et comportement requis donnent
spy, l'interaction l'emportant. Les cas couvrent les quatre feuilles.

Le coût est constant ; la valeur est le vocabulaire. La transposition est immédiate en revue
de tests : devant chaque simulacre, demander « quel besoin ce double sert-il ? » — une
réponse, un comportement, une preuve d'appel — et remplacer les doubles sur-outillés par le
plus simple qui suffit. Les suites de tests qui survivent aux refactorisations sont celles où
chaque double a été choisi par cette question, pas par la bibliothèque du jour.
