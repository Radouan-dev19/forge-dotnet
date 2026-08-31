# Scripts d'entretien : zones sensibles, récits et questions à poser

Un cofondateur d'une société de services évalue une chose avant tout : peut-il vous placer chez
son client sans risque. Un profil fiable, clair sur son périmètre et motivé pour une mission
longue vaut mieux qu'un profil gonflé qui craque chez le client au premier sprint — et personne ne
le sait mieux que lui. Ces scripts servent un seul positionnement : solide en C#, .NET et SQL avec
une vraie migration en production, honnête sur le reste, et qui apprend vite — preuve à l'appui.

## Le principe : devancer, ne jamais subir

Chaque zone sensible se traite en trois temps : ce qui est vrai et solide, l'écart nommé par
vous-même avant qu'on vous y coince, et la preuve que l'écart se comble. Un écart assumé inspire
confiance ; un écart découvert par l'interlocuteur détruit tout le reste, y compris ce qui était
vrai.

## Zone sensible : les API

Réponse préparée : « En production, mon système de promotions est une application MVC consommée
par plusieurs applications internes. Je n'ai pas encore exposé d'API REST en production — c'est la
même mécanique de contrôleurs, avec des DTO et des codes de statut à la place des vues, et j'en ai
monté une en local cette semaine pour valider que la marche est petite. Je préfère être précis
là-dessus : c'est un écart qui se comble en semaines. » Si la conversation va plus loin : verbes
et idempotence, différence entre 200, 201, 400 et 404, corps de réponse en JSON, validation des
entrées côté serveur.

## Zone sensible : certifications et pipelines

Réponse préparée : « J'ai suivi l'intégralité du cursus des fondamentaux Azure ; je n'ai pas passé
l'examen, payant et non financé. Mes pipelines datent de ma formation ; au quotidien j'utilise la
plateforme DevOps pour le code, les tickets et le suivi. » Règle absolue : ne jamais revendiquer
une certification non passée — une vérification tue une candidature, un écart assumé non. Si le
CV transmis prête à confusion sur un point, c'est vous qui le corrigez en entretien, pas lui qui
le découvre.

## Zone sensible : le front-end et TypeScript

Réponse préparée : « Je suis back-end de formation. J'ai lu la syntaxe de la Composition API pour
suivre une revue de code Vue — valeurs réactives, valeurs calculées, props typées — et ce poste
est exactement l'occasion d'apprendre le front moderne en production, encadré par une équipe qui
le pratique. » Ne pas s'étendre : une phrase de périmètre, une phrase d'envie, et retour sur le
terrain solide.

## Zone sensible : les années d'expérience

L'offre demande cinq ans ; la réponse tient en une phrase : « Trois ans en comptant l'alternance,
mais avec une migration d'un système patrimonial menée en production de bout en bout — c'est
souvent ce que la mission exige réellement. » Puis enchaîner immédiatement sur le récit de la
migration : la meilleure défense d'un compteur d'années est une histoire qui montre la maturité.

## L'ossature des trois récits

**La migration.** Situation : un ERP interne en WebForms, coûteux à faire évoluer. Tâche : le
porter vers .NET 8 sans arrêt du service. Action : isoler des modules aux frontières nettes,
les tester et les livrer séparément — le motif de l'étrangleur, sans forcément le nommer d'abord.
Résultat : gain de performance mesuré, base saine pour la suite. Ce récit répond à la fois à
« digitaliser des processus », à « microservices » et à « du cahier des charges aux
spécifications ».

**L'automatisation.** Situation : des campagnes de promotion gérées à la main dans plusieurs
applications. Tâche : centraliser. Action : un service unique consommé par les applications,
règles en base, procédures optimisées. Résultat : environ soixante-dix pour cent de temps gagné
sur les campagnes. Une phrase de correction honnête si besoin : consommé en interne via MVC, pas
exposé en API publique.

**Le cycle complet.** Situation : une application de gestion budgétaire à construire. Tâche :
partir du besoin métier. Action : analyse, spécifications fonctionnelles et techniques,
développement, maintenance et support. Résultat : application en service, évolutions livrées.
C'est le récit qui prouve la ligne « de l'analyse aux spécifications d'implémentation ».

## Le positionnement salaire

À garder pour la fin, formulé en évolution : « Je préfère entrer un cran en dessous et être
réévalué à six mois sur les compétences Azure et Vue acquises en mission. » C'est une proposition
qui rassure l'employeur sur le risque et vous donne un rendez-vous de renégociation daté — bien
plus solide qu'une concession vague.

## Les questions à poser

Elles montrent l'esprit d'initiative demandé par l'offre : taille et composition de l'équipe
SCRUM chez le client ; comment se passe la revue de code — qui relit, avec quels critères ; qui
écrit les spécifications et comment elles arrivent aux développeurs ; la stratégie de tests
automatiques en place ; le rythme des mises en production ; et comment la société accompagne la
montée en compétences de ses délégués. Terminer par ce qui a été préparé cette semaine —
messagerie, microservices, bases de Vue — dit mieux que toute déclaration que l'initiative est
déjà là.
