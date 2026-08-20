# Agents et sous-agents : déléguer sans perdre la main

Un agent est un assistant qui boucle seul — lire, agir, constater, recommencer — jusqu'à un objectif.
Un sous-agent est un agent lancé par votre session principale, avec son propre contexte jetable.
Bien employés, ils démultiplient un développeur ; mal employés, ils produisent vite, faux, et cher.
La compétence n'est pas de les lancer, c'est de savoir **quoi** leur donner et **comment vérifier**
ce qui revient.

## Pourquoi déléguer : les deux vrais bénéfices

**L'isolation du contexte.** Le premier bénéfice n'est pas la vitesse, c'est la propreté : une
fouille de vingt fichiers, une sortie de build de mille lignes, un essai raté — tout cela encombre
le contexte de celui qui le fait. Un sous-agent l'absorbe dans sa fenêtre à lui et ne rapporte que
la conclusion ; votre session principale — celle qui porte les décisions — reste courte et lucide.

**Le parallélisme.** Trois recherches indépendantes, quatre fichiers à migrer selon le même patron,
plusieurs pistes de correction à comparer : lancés en parallèle, des sous-agents rendent en un tour
ce qu'une session séquentielle rendrait en cinq. La condition est l'indépendance réelle : deux
agents qui modifient les mêmes fichiers se marchent dessus — les outils sérieux offrent pour cela
des copies de travail isolées du dépôt, une par agent.

## Les quatre patrons qui couvrent presque tout

1. **L'explorateur.** Question large, réponse courte : « où ce dépôt gère-t-il les migrations, et
   selon quel patron ? » L'agent lit beaucoup, vous recevez trois paragraphes. À utiliser dès qu'une
   réponse exige de parcourir plus de fichiers que vous ne voudriez en voir défiler.
2. **L'implémenteur cadré.** Une tâche fermée avec critère de fin exécutable : « fais passer ce test
   rouge sans toucher aux autres fichiers ». Plus le critère est mécanique, plus la délégation est
   sûre — un agent sans critère de fin vérifiable dérive.
3. **Le vérificateur adversarial.** L'agent le plus sous-employé : donnez-lui du code — le vôtre ou
   celui d'un autre agent — avec la consigne inverse, « trouve ce qui casse, essaie de réfuter ».
   Séparer l'auteur du critique compte double avec des modèles : un même contexte défend
   naturellement ce qu'il vient de produire. Le harnais de test de ce dépôt applique ce patron à
   grande échelle, et la piste senior vous a appris son critère : un défaut ne compte que reproduit.
4. **Le pipeline.** Enchaîner : explorer, puis transformer, puis vérifier — chaque étage dans son
   contexte, la sortie de l'un nourrissant l'autre. Réservez-le aux travaux de masse (migrations,
   audits) : sa mise au point coûte, il se rentabilise sur le volume.

## Ce qui se délègue mal

Les décisions dont les conséquences sont lointaines (architecture, contrats publics, sécurité), les
tâches sans critère de vérification mécanique, et tout ce qui exige le contexte que seul vous avez —
la politique de l'équipe, l'historique des choix, l'intention du produit. La frontière est la même
que pour tout collaborateur : on délègue l'exécution vérifiable, on garde le jugement.

## Le coût réel, souvent sous-estimé

Chaque sous-agent repart de zéro : il refacture la lecture du projet que votre session avait déjà
payée, et cinq agents lancés par confort coûtent plus qu'une session soignée. Trois garde-fous :
donner à l'agent un point de départ précis (fichiers, commandes) plutôt que le laisser redécouvrir ;
préférer un agent pour dix items à dix agents pour un item ; et jamais d'agent pour ce qu'un accès
direct fait en une seconde — lire une valeur dans un fichier connu ne se délègue pas.

## La règle qui ne se négocie pas

**Aucun résultat d'agent n'entre dans votre code sans vérification indépendante** — tests exécutés,
diff relu, ou second agent adversarial, et pour tout ce qui compte : les trois. Un rapport d'agent
est une déclaration ; vous avez appris ici même ce que vaut une déclaration face à une preuve. Les
équipes qui se font mordre par les agents sont exactement celles qui ont accepté « l'agent dit que
c'est fait » comme critère de fin. Le vôtre est : le build passe, les tests passent, et vous avez lu
le diff.
