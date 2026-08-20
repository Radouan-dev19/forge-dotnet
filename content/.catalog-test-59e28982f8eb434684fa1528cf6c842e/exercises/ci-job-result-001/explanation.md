# Explication

Un travail d'intégration réussit si la construction *et* les tests réussissent : la conjonction
tient en une expression, et l'exercice porte sur ce que ce verdict binaire *signifie* pour ceux
qui le lisent — la question de l'énoncé : que fait croire un travail annoncé réussi avec des
tests rouges ?

Il fait croire que la branche est saine. Le badge vert est le signal sur lequel toute l'équipe
s'appuie sans y penser : on tire la branche, on construit dessus, on déploie ce qu'elle
produit. Un vert menteur — construction passée, tests échoués, verdict « réussi » quand même —
empoisonne ce contrat : les régressions entrent dans la branche principale sous pavillon vert,
et se découvrent des jours plus tard, mêlées à d'autres changements, dix fois plus chères à
localiser. Le mensonge inverse — rouge alors que tout va bien — coûte aussi, en confiance :
une chaîne qui crie faux finit ignorée, et le jour où elle crie juste, personne n'écoute. Le
verdict d'un travail est un instrument de mesure ; sa calibration est la conjonction stricte.

Comment un vert menteur arrive-t-il en pratique ? Rarement par une conjonction mal écrite —
plutôt par sa dilution : l'étape de tests marquée « peut échouer » pour débloquer une urgence
et jamais remise, le code de sortie avalé par un script qui enchaîne les commandes sans
propager l'échec, le rapport de tests généré mais jamais lu par la chaîne. La fonction de
l'exercice est la règle nue — deux signaux, conjonction, verdict — et le fichier de flux du
laboratoire de livraison du parcours montre sa mise en œuvre réelle, où chaque étape doit
propager son échec pour que les signaux d'entrée de cette règle soient vrais.

L'ordre des deux signaux dans la conjonction est indifférent au verdict, mais pas à
l'exécution réelle : les tests d'un artefact qui ne construit pas n'existent pas — la chaîne
réelle court-circuite, et le signal de tests absent *est* un échec. La fonction pure reçoit
des booléens déjà établis ; savoir d'où ils viennent est la moitié de la compétence.

Les quatre combinaisons s'énumèrent et les cas les couvrent : seule la double réussite rend le
succès — les trois autres, y compris la construction seule, rendent l'échec. Domaine booléen
fini, couvert en entier, exception assumée du catalogue.

Le coût est constant. La transposition est la discipline des verdicts agrégés : chaque fois
qu'un signal résume plusieurs vérifications — santé d'un service, statut d'une migration,
résultat d'une sauvegarde —, la règle d'agrégation est une conjonction dont chaque
affaiblissement doit être une décision visible, datée et temporaire. Les badges verts ne
valent que ce que vaut leur règle.
