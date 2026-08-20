# Explication

Le verdict binaire « durci ou pas » a une vertu — il ferme une porte de déploiement — et une limite :
il ne dit pas quoi faire. L'audit qui le remplace ici produit un plan de travail, et chacun de ses
choix de conception — relevé complet, gravités différenciées, ordre stable, refus des configurations
partielles — répond à un besoin opérationnel précis.

**Pourquoi le relevé est complet plutôt qu'arrêté au premier écart.** L'arrêt au premier défaut
suffit pour refuser ; il condamne l'équipe à un cycle corriger-relancer-découvrir, un écart par
itération. Le relevé complet permet de corriger en un lot et, surtout, de négocier en connaissance :
si l'équipe doit vivre une semaine avec un écart, autant que ce soit celui de gravité moyenne, pas un
critique découvert à la relance suivante.

**Pourquoi les gravités sont différenciées, et pourquoi celles-là.** Les deux écarts critiques —
l'identité racine et l'élévation autorisée — partagent une propriété : ils transforment toute
compromission du processus en compromission du conteneur entier, et préparent l'évasion. Un attaquant
qui a un pied dans un processus racine n'a plus d'étape suivante à franchir ; un attaquant qui peut
élever ses privilèges s'en fabrique une. Les deux écarts hauts — la pile réseau de l'hôte et les
capacités par défaut — élargissent la surface sans donner directement les clés : voir tout le réseau
de l'hôte, disposer de capacités dont le processus n'a pas besoin. Le système de fichiers
inscriptible ferme la marche : il facilite la persistance d'un intrus déjà entré, mais n'aide pas à
entrer. Cette hiérarchie n'est pas décorative — elle est l'ordre de correction, et un audit qui donne
la même gravité à tout délègue ce tri à la personne la moins informée : celle qui lit le relevé.

**Pourquoi l'ordre de sortie est celui du référentiel et non de l'entrée.** Deux audits de la même
configuration, décrite dans deux ordres différents, doivent produire le même relevé au caractère
près : c'est la condition pour comparer les relevés entre eux, les mettre en différentiel d'une
semaine à l'autre, et détecter la régression — l'écart qui réapparaît. Un relevé dont l'ordre dépend
de l'entrée est un relevé qu'on ne peut pas suivre dans le temps.

**Pourquoi un réglage manquant est refusé plutôt que présumé durci.** La présomption de conformité
est le défaut classique des audits déclaratifs : ce que le fichier ne mentionne pas passe. Or
l'absence d'un réglage signifie le plus souvent la valeur par défaut de la plateforme — précisément
celle que le durcissement corrige. Certifier ce qu'on n'a pas vu, c'est signer un relevé faux ; le
refus force la description complète, et le coût de cette exigence est de quelques caractères.

La transposition : tout contrôle de conformité — en-têtes de sécurité d'une réponse, options d'un
compilateur, politique d'un compartiment de stockage — gagne la même structure : référentiel ordonné,
relevé complet, gravités qui décident de l'ordre de correction.
