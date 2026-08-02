# Guide de contenu

## Principes

Le contenu doit être autonome, concis, exact, testable et utile au marché .NET. Aucun lorem ipsum. Les ressources externes sont facultatives. Chaque fichier UTF-8 possède un identifiant stable, une version de schéma et une attribution/licence si nécessaire.

## Organisation

```text
content/
  curriculum/{path-id}.json
  curriculum/lessons/{lesson-id}/lesson.json
  curriculum/lessons/{lesson-id}.md
  exercises/{exercise-id}/exercise.json
  exercises/{exercise-id}/starter/
  exercises/{exercise-id}/tests/visible/
  exercises/{exercise-id}/tests/hidden/
  debugging/{scenario-id}/scenario.json
  sql/{scenario-id}/scenario.json
  interviews/*.json
  english/*.json
  projects/{project-id}.json
```

JSON est retenu pour les métadonnées validables sans dépendance additionnelle ; Markdown pour le corps pédagogique. YAML pourra être supporté plus tard, mais un seul format canonique évite les divergences.

Les contrats v1 autoritatifs, conventions de chemins et commandes de validation sont documentés dans `CONTENT_SCHEMA_V1.md`. Pour une leçon v1, le manifeste latéral `lesson.json` référence le Markdown ; le front matter YAML reste exclu.

## Format d'une leçon complète

En-tête JSON référencé par le Markdown ou front matter validé :

```json
{
  "schemaVersion": 1,
  "id": "csharp-types-001",
  "version": 1,
  "title": "Choisir un type adapté",
  "week": 1,
  "skills": [{ "id": "csharp.types", "weight": 1.0 }],
  "prerequisites": [],
  "estimatedMinutes": 75,
  "objectives": ["Choisir et justifier un type C# pour une donnée métier"],
  "sections": ["intuition", "explanation", "example", "counterExample", "check", "guided", "independent", "debugging", "interview", "summary", "reviewCards", "masteryTest"],
  "markdownPath": "lesson.md",
  "license": "CC-BY-4.0"
}
```

Le Markdown contient obligatoirement, dans cet ordre logique : objectif observable, prérequis, explication autonome, intuition, exemple commenté, contre-exemple/erreur fréquente, compréhension, exercice guidé, exercice autonome, débogage, entretien, résumé, cartes et test de maîtrise.

## Format d'un exercice automatisable

```json
{
  "schemaVersion": 1,
  "id": "csharp-orders-total-001",
  "version": 1,
  "title": "Calculer un total de commande",
  "kind": "csharp",
  "difficulty": 2,
  "skills": ["csharp.collections", "logic.edge-cases"],
  "prerequisites": ["csharp-types-001"],
  "estimatedMinutes": 35,
  "statement": "statement.md",
  "constraints": ["Ne pas modifier la signature publique"],
  "examples": [{ "input": "...", "output": "..." }],
  "reflectionFields": ["reformulation", "inputs", "expectedOutput", "edgeCases", "hypothesis", "plan"],
  "starterPath": "starter/",
  "visibleTestsPath": "tests/visible/",
  "hiddenTestsPath": "tests/hidden/",
  "hints": [
    { "level": 1, "kind": "socratic", "content": "..." },
    { "level": 2, "kind": "location", "content": "..." },
    { "level": 3, "kind": "strategy", "content": "..." },
    { "level": 4, "kind": "partial-pseudocode", "content": "..." }
  ],
  "solution": { "path": "solution/", "unlock": { "seriousAttempts": 2, "minimumDelayMinutes": 10 } },
  "explanation": "explanation.md",
  "complexity": "O(n) time, O(1) extra space",
  "commonMistakes": ["..."],
  "variantId": "csharp-orders-total-002",
  "reviewCards": ["card-csharp-orders-001"],
  "interviewQuestionId": "interview-csharp-012",
  "license": "CC-BY-4.0"
}
```

Les tests cachés ne sont jamais servis au navigateur. Le manifeste public indique seulement leur nombre et les catégories évaluées. Toute solution consultée déclenche l'état non maîtrisé et la révision.

## Tentative sérieuse

Une tentative est sérieuse si les champs de réflexion sont complets selon des minima transparents, si une modification substantielle du squelette existe, si la compilation ou les tests ont été lancés, et si elle n'est pas un doublon exact. Cette règle réduit les abus sans prétendre détecter parfaitement la triche.

## Explications déterministes

Chaque activité qui exige une explication définit une rubrique : concepts obligatoires, synonymes acceptés, contradictions, exemples attendus et score par critère. Les mots-clés seuls ne suffisent pas : au moins un lien causal et une méthode de test sont requis. Le résultat présente les critères manquants ; un LLM optionnel ne peut pas modifier seul la maîtrise.

## Débogage

Chaque scénario fournit dépôt cassé, ticket, attendu, logs, checklist, questions d'observation, correction et test de non-régression. Le journal exige symptôme, contexte, hypothèses, preuves, cause, correction, test et prévention. La validation porte autant sur la méthode que sur le correctif.

## SQL

Chaque scénario décrit image/dataset versionnés, schéma visible, droits, instruction de reset, résultat attendu, ordre significatif ou non, tolérances, timeout, nombre maximal de lignes et validation des effets. Les solutions expliquent plan, index ou transaction sans dépendre d'un coût exact.

Le lot initial 06B ajoute un contrat serveur `tests/contract.json` par scénario. Ce contrat contient les lignes attendues, la variante négative, l'invariant de reset et les propriétés spécialisées éventuelles ; il n'est jamais publié avec la projection apprenant. Les exemples EF compilables restent dans `starter/` et `solution/`, partagent seulement un modèle pédagogique isolé et ne dépendent pas de la base SQLite de progression. La matrice et les commandes de revue sont détaillées dans `SQL_EF_CONTENT.md`.

## Cartes et entretiens

Une carte contient question, réponse attendue, distracteurs facultatifs, compétence, source et intervalle initial. Une question d'entretien contient niveau, durée, critères observables, réponse modèle, erreurs fréquentes et variantes ; elle évite les trivia rares.

## Validation automatisée

Le validateur doit vérifier par incréments : schéma, unicité/stabilité des IDs, chemins confinés à `content/`, sections obligatoires, quatre indices ordonnés, solution et variante, tests visibles/cachés, pondérations valides et Markdown non vide en 02A ; références, prérequis sans cycle et volumes par lot avec le catalogue en 02B ; compilation/exécution avec les incréments de pratique et de runner. Une validation partielle n'est jamais présentée comme une validation complète.

## Revue éditoriale

Checklist : exactitude technique, objectif observable, charge réaliste, vocabulaire défini, exemple exécutable, cas limite, accessibilité, absence de dépendance externe, solution distincte de l'énoncé, test anti-contournement, anglais naturel et licence compatible.

## Évolution

Les corrections éditoriales incrémentent `version`. Une rupture de structure incrémente `schemaVersion` et fournit un migrateur/diagnostic. Les tentatives gardent le hash de la version utilisée ; elles ne sont jamais réinterprétées silencieusement.
