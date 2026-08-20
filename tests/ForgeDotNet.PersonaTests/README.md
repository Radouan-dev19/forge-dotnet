# Rejeu navigateur des sept personas de l'audit

Ce projet pilote un Chromium réel avec Microsoft.Playwright pour rejouer les sept scripts gelés de
`docs/PEDAGOGICAL_AUDIT.md` — la première des deux conditions de levée du verdict. Chaque persona
démarre l'application en processus enfant sur un port libre, avec un dossier de données SQLite dédié
et jetable, tente ses contournements avant les chemins nominaux, puis range ses preuves — captures
d'écran horodatées et assertions sur l'état persistant — sous `artifacts/personas/<horodatage>/`,
dossier non versionné, avec un registre Markdown par persona.

## Exclusion volontaire de la suite par défaut

Le projet n'appartient pas à `ForgeDotNet.sln` : `dotnet test` à la racine ne le lance jamais. Il
exige un navigateur Playwright installé et, pour les personas qui valident du code (P2, P3, P4, P5,
P7), un moteur Docker actif avec l'image runner construite. P4 exige de plus le conteneur SqlLab.

## Installation — le seul moment réseau

Comme l'installation npm des laboratoires front, l'installation du navigateur Playwright est un
téléchargement réseau ponctuel, à versions épinglées (`Microsoft.Playwright` 1.49.0 dans
`Directory.Packages.props`, Chromium fourni par cette version) :

```powershell
dotnet build tests/ForgeDotNet.PersonaTests
pwsh tests/ForgeDotNet.PersonaTests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Aucune autre étape ne touche le réseau. Les exécutions suivantes sont locales.

## Lancement

```powershell
# Prérequis des personas validants :
powershell -ExecutionPolicy Bypass -File scripts/build-code-runner.ps1
powershell -ExecutionPolicy Bypass -File scripts/start-sql-lab.ps1

# Les sept personas (P3 attend réellement le délai serveur de dix minutes) :
dotnet test tests/ForgeDotNet.PersonaTests

# Un persona isolé :
dotnet test tests/ForgeDotNet.PersonaTests --filter "FullyQualifiedName~P6"
```

Les personas s'exécutent séquentiellement (`xunit.runner.json`) : chacun possède son port, son
dossier de données et son navigateur, mais le moteur Docker et le poste restent partagés.

## Les sept personas

| Test | Script gelé | Mode CodeRunner |
|---|---|---|
| `P1DebutantFragileTests` | Diagnostic pauvre, plan borné, quiz raté sans progression, réflexion vague refusée | Manual |
| `P2TricheurTests` | Quiz répétés, déclarations, boucle, indices, verrou d'examen, portes | Docker |
| `P3ConsommateurDeSolutionsTests` | Solution avant réflexion, doublons, délai réel de dix minutes, contamination | Docker |
| `P4FaibleSqlTests` | SqlLab indisponible puis isolé, inter-base refusé, porte non compensée | Docker + SqlLab |
| `P5FortQuizFaiblePratiqueTests` | Lecture forte, deux réussites dont une assistée, récence vieillie | Docker |
| `P6SansDockerTests` | Parcours consultatif, export public, mode manuel sans preuve | Manual |
| `P7RetourApresDeuxSemainesTests` | Arrêt complet, +14 jours simulés, reprise sans perte ni pénalité | Docker |

## L'avance d'horloge de P5 et P7 — limite assumée

Le produit n'expose **aucune horloge de test** : une horloge réglable côté produit serait un canal
de falsification de récence, exactement ce que la politique de preuve interdit. L'horloge système
n'est jamais modifiée non plus. L'avance de quatorze jours (P7) et le vieillissement de trente et un
jours (P5) sont simulés par une **translation uniforme des horodatages persistés**, application
arrêtée, via `SqliteInspector.ShiftPersistedTimestamps` — l'équivalent déterministe documenté, que
chaque registre déclare explicitement.

Aucun persona ne produit de preuve de maîtrise : ces tests observent le produit, ils ne s'y
substituent pas.
