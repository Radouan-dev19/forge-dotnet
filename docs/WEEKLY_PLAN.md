# Plan personnalisé — incrément 03C

## Périmètre

L'incrément 03C transforme une évaluation diagnostique persistée en recommandations ordonnées et en proposition de progression sur 24 semaines. Le plan est local, explicable, ajustable avant acceptation et versionné dans SQLite.

La route disponible est :

- `/plan/{diagnosticSessionId}` : créer ou relire la proposition, modifier sa charge puis accepter une version.

Le plan n'exécute aucune activité, ne calcule aucune maîtrise et ne planifie aucune révision automatique. Les semaines décrivent des thèmes du curriculum ; elles ne prétendent pas que les contenus des incréments futurs sont déjà disponibles.

## Source de curriculum

`content/planning/v1/curriculum.json` est le snapshot de planification v1. Il reprend les 24 thèmes de `CURRICULUM.md`, leurs domaines diagnostiques et leurs prérequis. Le chargeur Infrastructure :

- confine le fichier sous `content/` ;
- impose UTF-8 strict, 64 Kio maximum et des propriétés JSON exactes ;
- exige 24 semaines numérotées sans trou ;
- refuse les identifiants dupliqués, domaines inconnus, prérequis absents, futurs ou cycliques ;
- exige la couverture des neuf domaines du diagnostic ;
- calcule une révision SHA-256 du fichier.

La proposition persiste ce snapshot. Une ancienne version reste donc lisible sans recharger ou réinterpréter le curriculum courant.

## Recommandations

`WeeklyPlanRules` est un calcul Domain pur. Chaque domaine reçoit exactement une recommandation :

1. lacune critique avec score observable : remédiation critique prioritaire ;
2. domaine sans observation, critique ou non : bases puis collecte de preuves sans prétendre mesurer une faiblesse ;
3. score inférieur à 50 : fondamentaux à reprendre ;
4. score de 50 à moins de 75 : repères à consolider ;
5. score d'au moins 75 : étude condensée, avec contrôle conservé.

Les lacunes critiques précèdent les recommandations non critiques et doivent apparaître dans au moins une semaine avec remédiation. Lorsqu'une lacune critique vient d'une preuve absente, elle conserve le libellé prudent « preuves à compléter » au lieu d'inventer un score. Une moyenne globale ne peut jamais les supprimer. Un domaine fort réduit la profondeur d'étude, jamais le contrôle prévu ; ce contrôle est une obligation du plan, pas un score de maîtrise déjà disponible.

## Charge hebdomadaire

La charge par défaut vaut le minimum entre les heures disponibles du profil et 15 heures.

- disponibilité de 10 à 15 h : charge identique aux disponibilités ;
- disponibilité supérieure à 15 h : plafond à 15 h avec avertissement ;
- disponibilité inférieure à 10 h : charge limitée aux disponibilités avec avertissement sur le rythme ;
- ajustement utilisateur : entier de 1 à `min(disponibilités, 15)` validé côté serveur.

Une version conserve les disponibilités qui ont servi à son calcul. Si le profil change avant acceptation, l'écran distingue cette valeur figée des disponibilités actuelles ; le prochain ajustement applique les bornes actuelles et les persiste dans une nouvelle version.

Chaque semaine répartit exactement la charge entre étude du thème, remédiation, consolidation et contrôle conservé. Les proportions dépendent de la recommandation prioritaire de la semaine. Une lacune critique réserve 30 % à la remédiation ; un domaine fort réduit l'étude du thème à 35 % et réalloue le temps à la consolidation. Le contrôle conserve 15 % dans tous les cas.

## Diagnostic incomplet

Un rapport à confiance faible ou insuffisante produit un plan explicitement provisoire. L'absence d'observation devient « preuves à compléter », jamais une faiblesse certaine. L'utilisateur peut accepter cette version, mais l'avertissement reste figé dans le snapshot.

## Versions et acceptation

`WeeklyPlans` contient une ligne par version et par diagnostic :

- identifiant du profil et de la session ;
- numéro et statut `Draft` ou `Accepted` ;
- identité et révision du curriculum ;
- charge cible ;
- snapshot JSON complet du plan ;
- dates UTC de création et d'acceptation.

La création initiale est idempotente. Chaque ajustement crée une nouvelle version sans supprimer les précédentes. Le client doit fournir la version attendue ; une version périmée est refusée. L'acceptation fige la dernière version et interdit tout nouvel ajustement.

## Sécurité et confidentialité

- Les mutations passent par le circuit Blazor protégé par antiforgery.
- Les valeurs de charge sont revalidées dans Domain et Infrastructure ; les bornes HTML ne sont pas une autorité.
- Le snapshot n'enregistre ni pseudonyme, ni objectif professionnel, ni réponse diagnostique, ni clé attendue.
- Aucun appel externe, télémétrie, recommandation d'emploi ou message culpabilisant n'est produit.
- Les erreurs de version, curriculum ou persistance sont affichées et échouent fermées.

## Vérification

```powershell
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
dotnet run --project src/ForgeDotNet.Web
```

Le parcours manuel compare au minimum un diagnostic fort, un profil avec lacune critique et un diagnostic incomplet. Il vérifie les justifications, les avertissements, une charge faible et élevée, la création d'une nouvelle version, son acceptation, sa relecture et l'absence de lien vers Practice ou un runner.
