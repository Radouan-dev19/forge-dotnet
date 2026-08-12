# Fiches d'exécution Forge.NET

## Ordre d'exécution

Suivre l'ordre numérique et alphabétique : `00`, `01A`, `01B`, `01C`, `01D`, `02A` jusqu'à `12`. Un incrément ne démarre que lorsque tous ses prérequis sont validés dans `docs/ROADMAP.md`. `NEXT_TASK.md` est la source courte indiquant le prochain travail autorisé.

La chaîne de livraison est : `00 → 01A → 01B → 01C → 01D → 02A → 02B → 02C → 03A → 03B → 03C → 04A → 04B → 04C → 04D → 05 → 06A → 06B → 07A → 07B → 07C → 08 → 09 → 10 → 11 → 12`. Les prérequis transverses explicités dans les fiches (notamment persistance, schémas et moteurs) s'ajoutent à cette chaîne et ne l'assouplissent pas.

`BACKLOG-POST-AUDIT.md` n'appartient pas à cette chaîne et **ne porte aucun numéro**. C'est une préparation au sens de la section « Préparations éventuellement parallèles » : le triage mesuré des défauts constatés après la reprise éditoriale, et un prompt d'exécution complet pour chacun. Rien n'y est autorisé à démarrer tant que l'incrément 12 reste refusé ; convertir un de ses éléments en incrément demande une décision humaine, l'allocation d'un numéro, une fiche au format des autres et la mise à jour de `NEXT_TASK.md`.

## Démarrer une tâche

1. Ouvrir uniquement la fiche indiquée par `NEXT_TASK.md`.
2. Lire intégralement `AGENTS.md` et tous les documents cités dans « Contexte à lire ».
3. Exécuter les contrôles préalables et vérifier `git status` sans écraser les changements existants.
4. Reformuler le périmètre et les exclusions ; ne pas commencer une tâche suivante.
5. Implémenter par petites étapes, avec les tests critiques avant ou avec le code.

## Valider ou refuser

Une tâche est validée uniquement lorsque tous les critères d'acceptation sont démontrés, toutes les commandes applicables réussissent et le rapport final contient les preuves demandées. Une erreur, un test ignoré sans justification, un contrôle de sécurité non démontré ou un placeholder présenté comme terminé impose le refus. La revue doit distinguer « conforme », « non conforme » et « non applicable avec justification ».

## Reprendre après un échec

Conserver l'incrément non validé. Lire le rapport et les sorties exactes, reproduire l'échec, corriger uniquement ce qui appartient au périmètre, ajouter un test de non-régression puis reprendre l'ensemble des vérifications. Ne jamais cocher la roadmap pour contourner un blocage et ne jamais avancer vers l'incrément suivant.

## Pourquoi ne pas paralléliser des incréments dépendants

Les contrats, migrations, schémas de contenu et règles de maîtrise changent les hypothèses des tâches suivantes. Les exécuter ensemble crée des conflits, masque les responsabilités et empêche d'attribuer une preuve à un incrément. La préparation parallèle ne vaut jamais validation et ne doit pas être fusionnée avant son prérequis.

## Préparations éventuellement parallèles

- Après validation de `02A`, des brouillons éditoriaux conformes au schéma peuvent être préparés pour `04D`, `05`, `06B`, `08`, `09` et `10`, dans des branches séparées, sans fusion avant leurs moteurs respectifs.
- Après validation de `04A`, la conception des doubles de runner `04B` et la préparation des cas d'abus `04C` peuvent être relues en parallèle ; `04C` reste dépendant de `04B`.
- Après validation de `07A`, les jeux de cas de révision `07B` et d'examen `07C` peuvent être préparés séparément ; `07C` ne fusionne qu'après `07B`.
- L'audit `12` doit rester indépendant de l'équipe ayant livré `11` et ne commence qu'après validation de `11`.

Dans tous les cas : une seule mutation structurante à la fois, aucune fusion anticipée et une nouvelle exécution complète des tests après intégration.
