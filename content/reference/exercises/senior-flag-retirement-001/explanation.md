# Explication

Les drapeaux de fonctionnalité sont une dette d'un genre particulier : contractée volontairement,
utile à la signature, et oubliée sitôt l'issue jouée. La base héritée typique en garde des dizaines,
et leur coût est diffus — chaque lecture traverse des branchements fantômes, chaque test couvre des
combinaisons qui n'existent plus, chaque nouvelle personne demande « et ce drapeau, il sert ? » sans
obtenir de réponse. Le retrait mérite donc la même rigueur que l'introduction, et l'audit de cet
exercice en fixe les trois règles.

**L'état mixte protège absolument, parce qu'il est la définition de l'utilité.** Un drapeau mixte
pilote encore des populations différentes — un déploiement progressif en cours, un contournement
client, un interrupteur d'urgence armé. Son âge ne dit rien : l'interrupteur d'urgence a
légitimement des années. Le retirer couperait un mécanisme actif, et aucun critère d'ancienneté ne
rachète cela. Si le registre entier est mixte, l'audit rend une chaîne vide — et cette vacuité est
elle-même une information : l'équipe n'assume jamais ses issues, ce qui se corrige par une politique
de cycle de vie, pas par un audit plus agressif.

**L'âge minimal encode le droit à l'inversion.** Une issue jouée depuis dix jours peut encore se
renverser — un incident tardif, un retour client, une métrique qui se dégrade lentement. Retirer le
drapeau à chaud transformerait chaque inversion en déploiement d'urgence au lieu d'un simple
basculement. Le seuil d'âge est le délai de rétractation de l'équipe ; sa frontière est inclusive —
l'âge qui atteint le minimum suffit — parce qu'un seuil est une promesse, pas une marge.

**Les deux sorts sont deux chantiers opposés, et les confondre détruit.** Le drapeau allumé partout a
gagné : son code est la fonctionnalité vivante, et le retrait consiste à **intégrer** — supprimer le
branchement et l'ancienne voie, faire de la nouvelle la seule. Le drapeau éteint partout a perdu :
sa branche est du code mort, et le retrait consiste à **supprimer** — la branche part avec le
drapeau. Un audit qui rendrait un sort unique laisserait l'exécutant deviner, et les deux erreurs
possibles sont graves : détruire la voie vivante d'un drapeau gagnant, ou garder pour toujours la
branche morte d'un perdant. Le sort nommé fait de chaque ligne du rapport une tâche sans ambiguïté.

**L'ordre du registre, une dernière fois.** Les sorts sortent dans l'ordre d'entrée parce que le
rapport retourne au registre : chaque ligne s'annote, se transforme en ticket, se coche. Le tri
esthétique casserait cette correspondance.

En entretien, le sujet se nomme feature flag debt, et la question type est duale : « quand un
drapeau peut-il partir, et dans quel sens ? ». La réponse à deux conditions — issue jouée, issue
assumée — et deux sorts — intégrer, supprimer — couvre l'essentiel.
