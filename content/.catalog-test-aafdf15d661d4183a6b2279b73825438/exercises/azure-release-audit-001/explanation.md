# Explication

Ce projet répète qu'une preuve n'existe que vérifiée, et cet exercice en donne la mécanique côté
livraison : le jalon prêt n'est pas une déclaration, c'est un verdict calculé sur des pièces datées.
Les trois idées qui structurent l'audit — la fraîcheur, la distinction périmé-manquant, l'ordre du
référentiel — répondent chacune à une manière connue dont les dossiers de livraison mentent.

**La fraîcheur, parce qu'une preuve décrit un instant du code.** Des tests verts sont une
photographie : ils prouvent quelque chose sur le code tel qu'il était au moment de leur exécution.
Le correctif « d'une ligne » poussé ensuite invalide la photographie — pas parce qu'il est risqué,
mais parce que plus rien ne relie la preuve au code qui part. Le dossier qui présente des pièces
d'avant le dernier changement n'est pas incomplet, il est trompeur : tout y a l'air vert. Dater
chaque pièce par rapport au dernier changement, et ne compter que les fraîches, est la seule lecture
qui empêche le jalon de vivre sur des preuves d'un autre code. C'est le cliquet des plafonds de ce
dépôt appliqué à la livraison : la preuve se rejoue à chaque changement, elle ne s'hérite pas.

**Périmé et manquant se distinguent parce que leur correction diffère.** L'audit ne sert pas à
donner un score, il sert à déclencher le bon travail. Une revue de sécurité périmée se rejoue — le
protocole existe, les personnes sont identifiées, il faut une passe de plus ; une revue absente se
construit — il faut trouver le temps, les gens, le périmètre. Fondre les deux états dans un
« bloqué » générique oblige l'équipe à rouvrir le dossier pour comprendre ; les garder distincts
transforme le verdict en liste de tâches. Le choix d'accepter aussi la déclaration explicite d'une
pièce manquante va dans le même sens : un dossier honnête sur ses trous vaut mieux qu'un dossier
silencieux, et l'audit les traite pareil — mais il les affiche.

**L'ordre du référentiel, parce que les verdicts se comparent.** Le dossier arrive dans l'ordre où
on l'a assemblé ; le verdict sort dans l'ordre où la politique exige les pièces. Deux audits du même
jalon, dossiers mélangés différemment, rendent le même verdict au caractère près — condition pour
suivre un jalon dans le temps et voir une pièce fautive disparaître du verdict d'une semaine à
l'autre. Un verdict dont l'ordre dépend de l'entrée ne se compare pas, donc ne se suit pas.

**Le refus des pièces hors référentiel ferme la dernière échappatoire.** Le dossier gonflé — vingt
pièces fournies dont les trois exigées manquent — est une technique de soutenance connue : le volume
imite la rigueur. Refuser toute pièce que le référentiel n'exige pas garde le dossier lisible et la
discussion sur les seules pièces qui prouvent.

La transposition dépasse la livraison : dossier d'architecture, revue d'accès, bilan d'astreinte —
tout contrôle sur pièces gagne la même grammaire : référentiel fermé, pièces datées, verdict complet
et ordonné.
