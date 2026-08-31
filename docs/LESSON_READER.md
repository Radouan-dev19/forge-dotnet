# Lecteur de leçon

## Périmètre livré

L'incrément 02C fournit deux routes locales :

- `/learn` présente les modules et recherche les leçons par titre, objectif ou compétence ;
- `/learn/{lessonId}` affiche une leçon publiée, son sommaire, son quiz de compréhension, une note personnelle, un signet et une progression de lecture.

La leçon de référence `reference-types-001` couvre les quatorze rubriques imposées par le guide de contenu. Elle est autonome, mais ne fournit ni solution déverrouillable, ni test caché, ni exécution de code, ni preuve de maîtrise.

## Flux et responsabilités

`BrowseLessons` et les cas d'usage du lecteur appartiennent à Application. Ils ne dépendent que de projections publiques du catalogue, de `ILessonContentSource` et de `ILessonUserStateRepository`. Infrastructure charge le Markdown et conserve l'état utilisateur dans SQLite. Web compose ces services et rend uniquement des modèles typés.

Au démarrage, l'hôte charge le catalogue puis analyse toutes les leçons publiées. Une leçon invalide empêche le démarrage : aucun document partiellement valide n'est publié.

## Contrat Markdown du lecteur

Une leçon publique doit respecter exactement cet ordre de titres de niveau 2 :

1. Objectif observable
2. Prérequis
3. Intuition
4. Explication
5. Exemple commenté
6. Contre-exemple et erreur fréquente
7. Vérification de compréhension
8. Exercice guidé
9. Exercice autonome
10. Débogage
11. Entretien
12. Résumé
13. Cartes de révision
14. Test de maîtrise

Le lecteur accepte les paragraphes, listes ordonnées ou non, blocs de code clôturés, texte fort, code en ligne et liens sûrs. Le quiz est déclaré dans un bloc `:::quiz` strict contenant un identifiant, une question, des options, l'index correct et deux retours. La réponse correcte reste dans le modèle serveur et n'entre jamais dans la projection publique initiale.

Depuis le 28 août 2026, un identifiant d'activité publié cité en code en ligne — `` `api-cors-origin-001` ``, `` `sql-covering-read-001` `` — est résolu par `BrowseLessons` (via `ILessonActivityLinkResolver`, implémenté par `CatalogLessonActivityLinkResolver`) en lien direct vers la page qui le sert : `/practice/{id}`, `/sql-lab?scenario={id}`, `/debug-lab/{id}`, `/projects/{id}`, `/labs/{id}`. Ces liens s'ouvrent dans un nouvel onglet pour revenir finir la leçon ; une autre leçon (`/learn/{id}`) reste une navigation du parcours, même onglet. Un mot de code qui n'est pas un identifiant publié reste du code. La page SqlLab présélectionne le scénario passé en requête, s'il est publié.

Le même jour, la résolution est partagée par `LessonActivityLinker` entre le socle (`BrowseLessons`) et la piste senior (`BrowseSeniorTrack`), dont les leçons citaient leurs exercices sans lien ; une leçon senior citée mène à `/learn-senior/{id}` (préfixe `senior-`) ; un laboratoire cité par son identifiant ou par le chemin d'un de ses fichiers (`content/labs/<labo>/…`) mène à `/labs/<labo>` via `ILabSource`. Le quiz senior est vérifié côté serveur (`BrowseSeniorTrack.CheckQuizAsync`) sans rien enregistrer. `LessonActivityReachabilityWebTests` lit chaque `lesson.md` publié et refuse toute activité citée qui n'est pas reliée sur la page servie — la règle de joignabilité descend ainsi de la famille au document.

`SafeMarkdownLessonParser.ParseDocument` lit un Markdown libre (guides Semaine 0, carrière, IA, cloud ; briefs de laboratoire et de projet ; documents servis sur `/docs/{nom}` par liste blanche — `ServedDocumentsOptions`) en sections typées rendues par `MarkdownDocument.razor`, au lieu d'un `<pre>` brut ; un document refusé par le parseur retombe sur le texte brut, jamais sur une erreur.

## Progression honnête

Une simple ouverture de page ne modifie rien. Le pourcentage est calculé sur les activités déclarées de la leçon : quatorze confirmations explicites de section et un quiz réussi. Les identifiants inconnus et les doublons sont ignorés. Le quiz incorrect affiche un retour mais n'ajoute aucune activité. Cette progression décrit uniquement la lecture ; elle n'est jamais présentée comme un score ou une maîtrise.

## Persistance locale

SQLite conserve séparément :

- la note, limitée à 4 000 caractères et enregistrée automatiquement après 500 ms ou à la perte du focus ;
- le signet, activé ou retiré explicitement ;
- les identifiants d'activité de lecture, insérés de manière idempotente.

Les écritures sont transactionnelles et partagent le verrou local de la base. Un rechargement de page ou un redémarrage du processus restitue l'état. Une erreur d'enregistrement reste visible dans l'interface et n'est pas convertie en succès.

## Sécurité du rendu

Le chemin demandé doit appartenir au catalogue publié, rester sous la racine canonique de contenu et ne traverser aucun point de réanalyse. Le fichier est lu en UTF-8 strict et sa taille est bornée à 256 Kio.

Le Markdown n'est jamais injecté comme HTML : le parseur produit des blocs et segments typés que Razor encode. Le HTML brut reste du texte, les liens `javascript:` sont déclassés en texte et seuls HTTPS, les chemins locaux et les fragments sont autorisés. Les fichiers de solution et de tests cachés ne sont jamais ouverts par la source du lecteur.

L'hôte ajoute une CSP restrictive, `X-Content-Type-Options: nosniff` et `Referrer-Policy: no-referrer`. Les mutations transitent par le circuit Blazor protégé par l'antiforgery ASP.NET ; aucune route HTTP publique de mutation n'est ajoutée.

## Vérification

Depuis la racine du dépôt :

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

La vérification manuelle doit couvrir recherche, sommaire, quiz incorrect puis correct, note, signet, progression, rechargement, redémarrage, clavier et largeur mobile. Le lecteur doit rester utilisable sans débordement horizontal et sans révéler de réponse correcte avant soumission.

## Limites

Cet incrément ne contient pas de diagnostic, d'évaluation de niveau, de plan personnalisé, de runner, d'indices, de solutions, de score de maîtrise, de révisions, d'examen ou de contenu multi-semaines.
