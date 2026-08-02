# Diagnostic initial — collecte 03A et évaluation 03B

## Périmètre

L'incrément 03A collecte les réponses dans une session locale, figée, chronométrée et reprenable. L'incrément 03B évalue uniquement une session terminée ou abandonnée avec un barème déterministe, versionné et prudent. L'évaluation ne calcule aucune maîtrise ; l'incrément 03C consomme ensuite son rapport agrégé dans un module de planification distinct décrit par `WEEKLY_PLAN.md`.

Les routes disponibles sont :

- `/diagnostic` : consignes, couverture, démarrage ou reprise ;
- `/diagnostic/session/{sessionId}` : minuterie, questions, sauvegarde, transition de section, abandon et clôture ;
- `/diagnostic/session/{sessionId}/evaluation` : rapport agrégé figé, disponible uniquement après la fin de la collecte.
- `/plan/{sessionId}` : proposition 03C distincte, disponible après la persistance du rapport.

## Banque versionnée

La banque `forge-diagnostic-initial` v1 contient 36 questions à choix unique. Chacun des neuf domaines — logique, C#, lecture de code, débogage, SQL, HTTP, Git, tests et anglais professionnel — possède une question facile, deux variantes intermédiaires et une question avancée.

Les fichiers sont séparés :

- `content/diagnostic/v1/questions.json` contient uniquement les questions et options publiques ;
- `content/diagnostic/v1/answer-key.json` contient la clé attendue privée ;
- `content/diagnostic/v1/rubric.json` contient les poids, domaines critiques et seuils publics du barème.

La clé n'est présente dans aucun modèle rendu au navigateur. Infrastructure l'associe au barème uniquement côté serveur, après la fin d'une session. Le rapport ne contient ni identifiant de question, ni option choisie, ni réponse attendue.

Au démarrage, Infrastructure vérifie UTF-8 strict, taille, propriétés exactes, identifiants uniques, domaines, difficultés, quatre options par question, couverture et correspondance complète de la clé. Un fichier invalide empêche le démarrage.

## Échantillonnage reproductible

Le diagnostic initial sélectionne 27 questions : une question par couple domaine/difficulté. Le diagnostic réduit sélectionne 9 questions : une variante intermédiaire par domaine.

Les domaines sont répartis en trois sections :

1. raisonnement et code : logique, C#, lecture de code ;
2. diagnostic technique : débogage, SQL, HTTP ;
3. livraison et communication : Git, tests, anglais professionnel.

La sélection et l'ordre utilisent une clé SHA-256 déterministe dérivée de la graine de session et des identifiants stables. Une même banque, version et graine produisent le même plan. Le plan public complet est sérialisé dans SQLite lors de la création : une reprise ne recharge ni ne régénère le tirage depuis la banque courante.

## États et transitions

Une session est `Active`, `Completed` ou `Abandoned`. Une section est `Pending`, `Active`, `Completed`, `Expired` ou `Interrupted`.

- La première section démarre avec la session.
- Une section suivante reste en attente jusqu'à une action explicite ; cette pause entre sections est autorisée.
- Une section active possède une échéance UTC absolue calculée côté serveur local.
- À l'échéance, la section devient expirée et aucune nouvelle réponse n'est acceptée.
- Terminer une section deux fois, sauvegarder deux fois la même réponse ou terminer deux fois la session reste idempotent.
- L'abandon interrompt la section active et conserve les réponses déjà enregistrées.
- La clôture n'est possible que lorsque toutes les sections sont terminées ou expirées.

Une collecte est complète uniquement si chaque question possède une réponse sauvegardée. Une session terminée avec une réponse manquante ou une section expirée reste affichée comme « collecte incomplète ». Ce libellé n'est pas une évaluation.

## Minuterie et reprise

`TimeProvider` est injecté dans Application. La date limite n'est jamais calculée à partir d'une horloge, d'un compteur ou d'un champ envoyé par le navigateur. Chaque lecture ou mutation rafraîchit l'état selon l'heure serveur avant d'accepter l'action.

L'interface actualise l'affichage chaque seconde, mais cette valeur est uniquement informative. Fermer, actualiser ou modifier le client ne change pas l'échéance persistée. Au retour, la section reprend avec l'échéance originale ou apparaît expirée.

Durées par défaut :

- diagnostic initial : 30 minutes par section, soit 90 minutes ;
- diagnostic réduit : 120 secondes par section.

Configuration locale :

```text
Diagnostic__InitialSectionDurationMinutes
Diagnostic__ReducedSectionDurationSeconds
```

Les durées acceptées vont d'une seconde à deux heures. La durée applicable est figée dans la session au démarrage.

## Persistance

`DiagnosticSessions` conserve profil local, banque/version/révision, mode, graine, plan figé, états, durée et échéances. `DiagnosticResponses` conserve une ligne par couple session/question avec l'option sélectionnée et l'instant de sauvegarde. `DiagnosticEvaluations` conserve une seule évaluation par session : identité et snapshot du barème sans clé, rapport agrégé et date de création.

Les écritures passent par les cas d'usage Application et le verrou local partagé. Les réponses sont des upserts : une nouvelle sélection remplace la précédente sans créer de doublon. Le plan et les réponses survivent à un redémarrage complet du processus.

## Formule et barème 03B

Le barème `forge-diagnostic-rubric` v1 applique les poids de difficulté 1, 2 et 3. Les poids de domaine sont : logique 0,8 ; C# 1,2 ; lecture 0,9 ; débogage 1,2 ; SQL 1,2 ; HTTP 1,1 ; Git 0,8 ; tests 1,1 ; anglais 0,7.

Pour chaque domaine :

```text
score_domaine = 100 × somme(poids_difficulté des réponses justes)
                       / somme(poids_difficulté des questions planifiées)
```

Le score global secondaire applique également le poids de domaine. Une non-réponse rapporte zéro au score ponctuel ; elle élargit cependant l'intervalle plutôt que d'être présentée comme une observation certaine. Tous les résultats sont bornés à 0–100 et arrondis à un chiffre après la virgule, à mi-chemin en s'éloignant de zéro.

L'intervalle utilise Wilson à 95 % (`z = 1,96`) avec l'effectif pondéré de Kish :

```text
n_effectif = somme(poids)² / somme(poids²)
```

La borne basse applique Wilson uniquement au poids effectivement répondu. La borne haute ajoute le poids manquant comme résultat encore inconnu. Sans réponse, l'intervalle est donc 0–100.

## Confiance, niveau prudent et lacunes

- confiance **insuffisante** : collecte incomplète ou domaine absent ;
- confiance **faible** : mode réduit complet, qui ne contient qu'une observation intermédiaire par domaine ;
- confiance **modérée** : diagnostic initial complet ; aucune confiance « élevée » n'est revendiquée avec cette banque limitée ;
- domaines critiques : C#, débogage, SQL, HTTP et tests ; un score inférieur à 50 ou l'absence d'observation crée une lacune critique ;
- une lacune critique force le niveau « fondamentaux à renforcer », même avec un score global élevé ;
- le mode réduit ne peut dépasser « repères en développement » ;
- les seuils de borne basse du diagnostic initial sont 35, 55 et 75 pour les niveaux en développement, opérationnel à confirmer et solide à confirmer.

Un diagnostic incomplet reçoit toujours le niveau « preuves insuffisantes ». Ces libellés ne constituent ni une certification, ni une mesure de maîtrise, ni une appréciation d'employabilité.

## Stabilité de version

La révision du barème couvre le fichier de pondération et la clé privée. Le premier calcul persiste le rapport et son snapshot ; les lectures suivantes utilisent ce résultat figé, même si le barème courant change. Si une ancienne session n'a pas encore de rapport et que sa révision de banque n'est plus disponible, l'évaluation échoue explicitement au lieu de réinterpréter silencieusement les réponses.

## Sécurité et confidentialité

- Les mutations utilisent le circuit Blazor protégé par antiforgery ASP.NET.
- Le serveur vérifie que la session appartient au profil local, que la question appartient au plan figé, que l'option est publique et que la section est encore active.
- Aucune réponse attendue, réponse utilisateur, question complète ou information sensible n'est journalisée.
- La projection d'évaluation publique contient uniquement scores et compteurs agrégés par domaine ; aucune réponse attendue, option choisie ou correction question par question.
- Une session active est refusée par le cas d'usage d'évaluation.
- Le rapport ne contient aucune recommandation de plan et n'affirme ni emploi, ni salaire, ni maîtrise.
- Aucune surveillance vidéo, collecte de frappe, détection de copier-coller ou autre surveillance intrusive n'est ajoutée.

## Vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

Le parcours manuel 03B compare au minimum un profil fort, un profil avec faiblesse critique et une collecte incomplète. Il vérifie la prudence des libellés, l'intervalle visible, l'absence de compensation d'une lacune critique et l'absence de recommandation directement calculée dans l'évaluation.
