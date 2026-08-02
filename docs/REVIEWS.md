# Révisions 07B

## Périmètre

L’incrément 07B transforme des difficultés déjà observées localement en cartes de révision planifiées. Il couvre la file du jour, les cartes personnelles et l’explication du prochain intervalle. Depuis 07C, les échecs d’un rapport d’examen terminé alimentent aussi une carte de récupération ; cette extension ne change ni politique ni calendrier et n’ajoute aucun lot massif de contenu.

Le contrat actif est figé par les valeurs suivantes :

| Élément | Valeur |
|---|---|
| Politique | `forge-reviews` |
| Version | `1` |
| Révision | `reviews-v1-20260729` |
| Fuseau civil | `Europe/Paris` |
| Calendrier général | J+1, J+3, J+7, J+14, J+30 |
| Calendrier de récupération | J+1, J+7, J+14, J+30 |

La première échéance part du jour civil local de l’événement source. Une réussite avance d’une étape ; un échec repart à J+1. Une réponse tardive repart du jour réel de la réponse : après deux semaines d’absence, aucune dette, série perdue ou pénalité ne s’ajoute. Le calcul Domain utilise `DateOnly`, un fuseau explicite et des instants UTC fournis par le `TimeProvider` injecté ; il ne dépend jamais de l’horloge système directement.

## Sources et preuves

| Source | Carte | Calendrier | Effet sur la maîtrise |
|---|---|---|---|
| échec d’un exercice C# | rappel de l’erreur puis autoévaluation | récupération | aucun |
| bug DebugLab non résolu | rappel du bug puis autoévaluation | récupération | aucun |
| erreur ou validation SQL échouée | rappel du diagnostic puis autoévaluation | récupération | aucun |
| question diagnostique ratée | choix publics figés, réponse privée | récupération | preuve de rétention seulement après vérification serveur |
| solution C# ou DebugLab consultée | restitution à blanc puis autoévaluation | récupération | aucun |
| item d’examen échoué après rapport final | reprise à blanc de la compétence, sans solution | récupération | aucun |
| carte personnelle | question/réponse textuelle locales | général | aucun |

Les sources sont dédupliquées par profil, identité, version, révision de source et révision de politique. Leur snapshot est immuable dans la carte : une source supprimée reste révisable et une nouvelle révision produit une autre carte. Les échecs répétés d’une même activité ne créent donc pas une file infinie.

Une autoévaluation pilote uniquement le calendrier. Elle ne devient jamais un score arbitraire. Seule une question diagnostique ratée, à choix et corrigée côté serveur peut produire une observation `ReviewEngine` pour la composante `SpacedRetention`. La consultation d’une solution reste explicitement non maîtrisée.

## Modèle et concurrence

`ReviewItems` conserve le snapshot de source, le recto, la réponse privée, la politique, l’étape et l’échéance. `ReviewAttempts` est append-only et conserve résultat, caractère vérifié, admissibilité à la maîtrise, échéance suivante et empreinte SHA-256 de la réponse. La réponse brute n’est pas persistée dans l’historique.

Chaque mutation exige la version courante de la carte. Deux réponses concurrentes ne peuvent créer qu’une tentative : la seconde échoue explicitement et l’utilisateur doit recharger la file. La génération est idempotente, y compris après redémarrage.

## Sécurité, confidentialité et ergonomie

- La projection de file omet toujours la réponse attendue ; celle-ci n’est révélée qu’après soumission.
- Les cartes personnelles et leurs réponses restent dans SQLite local ; aucune télémétrie externe n’est ajoutée.
- Questions, réponses et libellés sont bornés et refusent les caractères de contrôle inattendus ; Razor assure l’encodage de sortie.
- Aucune solution future ou clé diagnostique n’est chargée dans une projection publique avant l’événement source autorisé.
- L’interface annonce le fuseau, la politique, l’intervalle et le retard factuel. Elle n’affiche ni série quotidienne, ni culpabilisation, ni récompense addictive.
- Le calendrier ne se présente pas comme une preuve de maîtrise lorsque la réponse n’est pas vérifiée par le serveur.

## Parcours manuel de référence

1. Ouvrir `/reviews` sur une base sans événement : la file est vide, la politique et le fuseau restent visibles.
2. Créer une carte personnelle : elle apparaît à J+1, sans modifier la maîtrise.
3. Rendre la carte exigible puis répondre avec une casse et des espaces différents : la réponse est acceptée, révélée après soumission et la prochaine échéance est J+3.
4. Simuler un retard de quatorze jours : le retard est annoncé sans pénalité ; une erreur replannifie à J+1 depuis le jour de réponse.
5. Vérifier qu’aucun examen, dashboard complet, série ou message culpabilisant n’apparaît.

Le parcours a été rejoué le 29 juillet 2026 avec une base temporaire et une horloge contrôlée. Les données temporaires ont été supprimées après vérification.

## Vérifications reproductibles

```powershell
dotnet build --no-restore --disable-build-servers
dotnet test --no-build --filter "Category=ReviewScheduling" --disable-build-servers
dotnet test --no-build --disable-build-servers
dotnet format --no-restore --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

Les tests `ReviewScheduling` couvrent tous les intervalles, réussite, échec, solution consultée, déduplication, jours civils et changement d’heure, retard, concurrence, source disparue ou versionnée, horloge déterministe, confidentialité de la réponse et séparation entre planification et maîtrise. Tout échec applicable invalide l’incrément.

## Limites assumées

La génération lit les observations locales déjà persistées. Pour le diagnostic, elle exploite la dernière session évaluée compatible ; elle ne réécrit pas les anciennes banques. Les rappels d’action C#, DebugLab, SQL, solution consultée et échec d’examen sont autoévalués et ne constituent pas une correction sémantique. Une carte issue d’examen reprend seulement le titre figé de l’item et son domaine : elle n’embarque ni code soumis, ni solution, ni test caché. Les mesures de dashboard appartiennent à la projection séparée de 07C et le contenu final des cartes reste réservé aux incréments de contenu.
