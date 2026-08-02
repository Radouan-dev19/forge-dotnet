# Spécification produit — Forge.NET

## Vision

Forge.NET est un environnement local, mono-utilisateur et sans API payante qui transforme l'étude en preuves vérifiables : résoudre, coder, déboguer, tester, expliquer et réviser sans aide. Le parcours principal dure 24 semaines à raison de 10 à 15 heures par semaine.

## Utilisateur cible

Développeur C#/.NET ayant environ trois ans d'expérience, surtout sur ERP/applications métier, à l'aise dans les scénarios connus mais fragile en algorithmique, SQL, débogage méthodique et autonomie. Il prépare un changement d'entreprise, notamment vers la Suisse, tout en renforçant son anglais professionnel.

## Principes produit

1. La pratique sans aide vaut davantage qu'un quiz ou une tentative assistée.
2. Une compétence critique insuffisante ne peut pas être compensée par une moyenne globale.
3. Toute affirmation de préparation est accompagnée de preuves et de lacunes.
4. Le contenu principal est autonome et utilisable hors ligne.
5. L'IA reste optionnelle ; l'accès aux indices impose une réflexion structurée.
6. Le système est exigeant sans mécanisme culpabilisant.

## Objectifs et non-objectifs

Objectifs MVP : diagnostic, parcours recommandé, leçon complète, exercices C#/SQL/débogage, indices progressifs, examen sans aide, maîtrise, révisions, tableau de bord, sauvegarde/restauration.

Non-objectifs MVP : réseau social, marketplace, mobile natif, multi-utilisateur, génération LLM obligatoire, certification officielle, microservices, promesse d'emploi ou de rémunération.

## Parcours principaux

### Première utilisation

1. L'utilisateur crée son profil local et accepte le contrat d'apprentissage.
2. Il effectue un diagnostic chronométré de 90 à 120 minutes.
3. Le système affiche compétences, niveau prudent, lacunes critiques et limites de la mesure.
4. Un plan hebdomadaire est proposé, puis explicitement accepté.

### Apprentissage quotidien

1. Le tableau de bord présente révisions dues et prochain objectif.
2. La leçon expose objectif, prérequis, explication, pratique et test.
3. Avant un indice, l'utilisateur renseigne reformulation, entrées, sortie, cas limites, hypothèse et plan ; observation du débogueur si applicable.
4. La tentative produit un résultat, une preuve et une prochaine action.

### Solution consultée

1. La solution n'est disponible qu'après deux tentatives sérieuses et le délai configuré.
2. L'activité devient « vue, non maîtrisée ».
3. Une explication personnelle et une variante sont exigées.
4. Une réimplémentation à blanc est planifiée à J+1 puis contrôlée à J+7.

### Examen sans IA

1. Le système tire des exercices compatibles, fixe une durée et verrouille indices/solutions.
2. Les résultats détaillés et tests cachés restent invisibles jusqu'à la fin.
3. Le rapport distingue correction, autonomie, explication et compétences critiques.

### Débogage

Le laboratoire guide : reproduire, décrire, définir l'attendu, réduire, hypothétiser, observer, confirmer, corriger, tester, documenter la cause racine.

### Preuves d'autonomie

Une page exportable liste uniquement les exercices sans aide, projets défendus, tests, rapports de bug, examens et commits compris. Elle distingue explicitement toute assistance.

## Fonctionnalités MVP

- Profil local et préférences, aucun compte cloud.
- Diagnostic, carte de compétences et plan personnalisé.
- Lecteur Markdown : navigation, quiz, notes, signets, glossaire, recherche et autosauvegarde.
- Exercices C# avec réflexion, historique, compilation/tests isolés et mode manuel honnête.
- Laboratoires SQL jetables et laboratoires de débogage avec journal.
- Maîtrise, répétition espacée, quatre portes d'employabilité et examens.
- Tableau de bord factuel et export/sauvegarde-restauration.
- Navigation clavier, contraste AA visé et interface responsive.

## Mesures honnêtes

- Temps actif, avec inactivité exclue après un seuil configurable.
- Réussite au premier essai et avant solution.
- Ratio d'activités sans aide ; niveau maximal d'indice utilisé.
- Rétention à J+1/J+7/J+30.
- Preuves récentes par compétence.
- Examens terminés et abandonnés.
- Aucune série quotidienne n'entre dans la maîtrise.

## Portes d'employabilité

| Porte | Critères minimaux |
|---|---|
| A — Junior fiable | C# ≥85, débogage ≥80, SQL ≥75, 10 exercices sans aide, mini-projet console, examen 90 min |
| B — Backend .NET | A, API fonctionnelle, EF Core, validation/erreurs, tests unitaires/intégration, Git propre, présentation 10 min |
| C — Équipe moderne | B, Docker, CI, authN/authZ, logs, déploiement, incident simulé, entretien blanc |
| D — Intermédiaire en construction | C, performance, sécurité, architecture pragmatique, fonctionnalité autonome, revue de code, anglais, défense du projet final |

Une porte reste fermée si un prérequis manque, quelle que soit la moyenne.

La politique de maîtrise v1 distingue ces minima de porte du seuil de validation d’un module : 80 pour un module ordinaire et 85 pour C#, débogage, SQL, API ou tests. Ainsi, le minimum SQL 75 de la porte A ne déclare pas à lui seul le module SQL maîtrisé.

## Critères d'acceptation MVP

Les 16 critères du prompt maître sont normatifs. Leur preuve prendra la forme d'une matrice dans le runbook : scénario reproductible, test automatisé associé lorsqu'il est pertinent, résultat attendu et lien vers l'artefact. Le MVP n'est accepté que si l'installation vierge est démontrée et tous les tests applicables sont verts.

## Hypothèses à valider

- Poste principal Windows 10/11 x64, Visual Studio ou CLI, Docker Desktop disponible pour le mode automatisé.
- Un seul profil actif dans le MVP, mais ses données possèdent un identifiant pour éviter une migration bloquante.
- Le français est la langue UI initiale ; l'internationalisation complète est différée.
- Le diagnostic initial utilise une banque versionnée et déterministe, sans surveillance vidéo.
- La désactivation du copier-coller est une friction UX, pas une garantie de sécurité.

## Décisions ouvertes non bloquantes

- Monaco Editor versus éditeur plus léger : mesurer poids, accessibilité et usage hors ligne pendant l'incrément CodeRunner.
- SQL Server Developer versus image Edge selon architecture hôte : choisir après test sur machines cibles.
- Export PDF natif versus impression navigateur : commencer par Markdown et impression, ajouter un moteur PDF seulement si nécessaire.
