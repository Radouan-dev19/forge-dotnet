# Audit pédagogique final — Forge.NET

## Statut et déclaration d'indépendance

Audit contradictoire en cours, pré-enregistré le 7 août 2026 avant tout essai produit de l'incrément 12. L'auditeur adopte explicitement une posture indépendante : les intentions, statuts historiques et affirmations de conformité ne valent pas preuve. Le verdict peut être refusé. Cet audit ne promet ni emploi, ni salaire, ni niveau professionnel certifié.

## Version auditée

Le dépôt comporte volontairement le travail non commité des incréments antérieurs. La référence auditée est donc le snapshot de travail, et pas seulement `HEAD` :

| Élément | Valeur gelée avant essai |
|---|---|
| Commit de base | `8b7c89da2560f94e59da91b349ef3cda9ec16da2` |
| Date du commit | `2026-08-02T17:40:06+02:00` |
| Empreinte Git du diff suivi | `e9f6b645667269b958747487769e581d7c4b55db` |
| Fichiers suivis modifiés | 90 |
| Fichiers non suivis | 795 |
| SHA-256 du manifeste trié des fichiers non suivis | `3ac0be629851665424669422968fe0028c5044e32f8c81c5ce555757df72e611` |

L'empreinte des fichiers non suivis est calculée sur `chemin relatif normalisé + SHA-256 du fichier`, triés par chemin. Les fichiers créés par l'audit et les corrections P0/P1 seront listés séparément afin de ne pas réécrire la référence initiale.

## Méthode pré-enregistrée

1. Lire l'ensemble des documents et fiches, puis vérifier le prérequis des 16 critères MVP.
2. Fixer les scripts, hypothèses adversariales, preuves attendues et conditions d'arrêt avant d'utiliser le produit.
3. Exécuter chaque persona sur une base et un dossier de données fictifs, dédiés et réinitialisables.
4. Croiser trois types de preuve : observation UI, état persistant/projection publique et test automatisé ciblé. Une affirmation documentaire seule ne suffit pas.
5. Tenter les contournements avant les chemins nominaux : score, aide, solution, examen, SQL, mode manuel et retour tardif.
6. Classer chaque défaut reproductible P0 à P3, sans corriger silencieusement P2/P3.
7. Corriger seulement P0/P1, ajouter une non-régression utile, puis rejouer le scénario et la suite complète.
8. Publier un verdict favorable uniquement si les sept personas sont exécutables, tous les P0/P1 sont clos et aucune sécurité n'a été affaiblie.

### Échelle de sévérité

| Niveau | Définition opérationnelle |
|---|---|
| P0 | Signal de maîtrise falsifiable à grande échelle, exposition de contenu protégé, atteinte à l'intégrité/sécurité ou parcours critique impossible sans solution sûre. Arrêt immédiat. |
| P1 | Blocage reproductible d'un persona, ambiguïté critique qui invalide l'évaluation, faux signal pédagogique majeur ou règle normative non appliquée. Correction obligatoire avant verdict favorable. |
| P2 | Défaut localisé avec contournement honnête, charge ou message sous-optimal sans faux score majeur. Backlog priorisé, sans correction massive dans cet audit. |
| P3 | Amélioration éditoriale ou ergonomique mineure, sans effet mesurable sur compréhension, accès ou intégrité des preuves. |

### Discipline des essais

- Données fictives uniquement ; aucun profil ou fichier de progression habituel n'est utilisé.
- Un dossier de données distinct est créé pour chaque persona et supprimé après collecte des preuves.
- Le CodeRunner et SqlLab conservent leurs images, réseaux, quotas, permissions et nettoyages documentés.
- Aucun test caché, solution ou clé diagnostique n'est copié dans le rapport.
- L'absence d'un humain indépendant disponible est déclarée ; elle n'est pas remplacée par une fausse interview humaine.
- Une limite temporelle couverte par une horloge serveur déterministe est testée automatiquement ; l'interface est néanmoins observée avant et après la transition.

## Scripts des sept personas

Les scripts ci-dessous sont gelés avant essai. Un scénario est « exécuté » seulement si chaque étape applicable produit une preuve référencée dans le registre final.

### P1 — Débutant fragile

**État initial.** Profil fictif avec 6 h disponibles, contrat accepté, aucune progression ; diagnostic réduit utilisé volontairement comme mesure pauvre.

**Hypothèses attaquées.** Une collecte partielle ou faible pourrait être formulée comme une maîtrise ; le plan pourrait surcharger, masquer l'incertitude ou supprimer un contrôle.

**Script.** Créer le profil ; lancer le diagnostic réduit ; omettre ou rater des réponses dans plusieurs domaines critiques ; terminer ou abandonner selon les transitions permises ; lire l'évaluation ; générer le plan ; essayer une charge hors bornes puis une charge valide ; accepter et relire ; ouvrir une leçon S1, échouer au quiz puis réussir ; commencer une pratique avec une réflexion volontairement vague.

**Preuves attendues.** Niveau « preuves insuffisantes » ou prudent, intervalle/incertitude visible, lacunes critiques non compensées, plan provisoire et compatible avec 6 h, contrôle conservé, quiz incorrect sans progression, réflexion insuffisante refusée avec aide actionnable.

**Arrêt propre au persona.** Un niveau affirmatif malgré collecte incomplète, une charge impossible imposée ou un faux statut de maîtrise est P0/P1.

### P2 — Tricheur

**État initial.** Profil fictif sans preuve, runner automatique disponible seulement dans l'environnement isolé prévu.

**Hypothèses attaquées.** Quiz répétés, attestations manuelles, répétition d'un item, aide puis réussite, faux examen ou rejeu de diagnostic pourraient ouvrir score ou porte.

**Script.** Répéter quiz et activités de lecture ; soumettre des déclarations manuelles ; boucler sur un même exercice ; utiliser H1 à H4 puis réussir ; tenter une solution ; rejouer les mêmes identifiants avec contenu divergent ; tenter de déclarer un examen/livrable ; pendant un examen actif, accéder directement à Practice et à ses mutations ; inspecter maîtrise et portes après chaque étape.

**Preuves attendues.** Quiz limité à 5 %, déclarations manuelles à zéro, variété de trois exercices requise, plafonds H1–H4, solution à zéro, rejeu divergent refusé, faux examen/livrable non admissible, Practice verrouillée durant l'examen, aucune porte ouverte par moyenne.

**Arrêt propre au persona.** Toute augmentation durable de maîtrise issue d'une preuve non vérifiée ou tout accès à une aide d'examen est P0.

### P3 — Consommateur de solutions

**État initial.** Profil fictif sur un exercice C# publié, aucune tentative préalable.

**Hypothèses attaquées.** La solution pourrait être accessible trop tôt, une duplication pourrait compter comme seconde tentative, ou l'activité redevenir maîtrisée immédiatement après copie.

**Script.** Demander solution et indices avant réflexion ; fournir les six champs ; soumettre une tentative trop courte, deux doublons, puis deux tentatives sérieuses distinctes ; demander la solution avant puis après le délai serveur ; la consulter ; soumettre une explication superficielle puis une explication causale et une variante distincte ; contrôler Practice, Reviews et Mastery ; essayer une réussite ultérieure du même exercice.

**Preuves attendues.** Accès prématuré refusé, doublons non sérieux, délai serveur conservé, état « solution consultée — non maîtrisée », explication superficielle refusée, carte de récupération planifiée, contamination définitive de l'exercice dans la politique v1 et aucune maîtrise immédiate.

**Arrêt propre au persona.** Solution prématurée, absence de révision ou réussite du même exercice effaçant la contamination : P0/P1.

### P4 — Faible SQL

**État initial.** Profil fictif avec fortes observations non SQL simulées par les producteurs serveur autorisés et échecs SQL réels dans une base SqlLab jetable.

**Hypothèses attaquées.** Une moyenne C#/quiz pourrait masquer SQL ; le moteur ou l'UI pourrait prétendre valider lorsque SqlLab est indisponible ; une session pourrait contaminer une autre ou la progression SQLite.

**Script.** Ouvrir SqlLab désactivé ; vérifier le message et l'absence de preuve ; démarrer SqlLab isolé ; exécuter une requête incorrecte puis une requête nominale sur un scénario publié ; tenter une requête inter-base/serveur ; reset ; comparer deux sessions ; consulter la maîtrise, le dashboard et les portes avec SQL sous seuil malgré les autres forces.

**Preuves attendues.** Mode indisponible honnête, erreur pédagogique actionnable sans fuite brute, attaque refusée, rollback/reset et isolation prouvés, faiblesse SQL visible, module critique non validé et porte concernée fermée sans compensation.

**Arrêt propre au persona.** Validation automatique en mode indisponible, accès inter-base/progression ou porte ouverte malgré SQL insuffisant : P0.

### P5 — Fort quiz, faible pratique

**État initial.** Profil fictif avec quiz et lecture élevés, sans trois exercices autonomes distincts, sans preuve récente et sans examen final vérifié.

**Hypothèses attaquées.** La complétion de cours ou les quiz pourraient être confondus avec autonomie et faire apparaître un libellé « prêt ».

**Script.** Compléter une leçon et plusieurs quiz ; produire au plus deux réussites automatiques, dont une assistée ; ne fournir ni troisième exercice, ni examen ; recalculer maîtrise ; ouvrir dashboard et quatre portes ; laisser une ancienne preuve dépasser la fenêtre de récence via horloge déterministe.

**Preuves attendues.** Lecture distincte de maîtrise, composante quiz bornée, pratique/retention/examen manquants à zéro, variété/récence explicitement bloquantes, forces/faiblesses fondées sur observations réelles et portes fermées.

**Arrêt propre au persona.** Module critique validé ou message de préparation sans preuves autonomes et examen : P0/P1.

### P6 — Sans Docker

**État initial.** Profil fictif, CodeRunner `Manual`, SqlLab désactivé, aucun moteur Docker requis pour le Web.

**Hypothèses attaquées.** Le produit pourrait bloquer sans issue, confondre export et validation, ou attribuer une tentative/preuve au mode manuel.

**Script.** Parcourir accueil, leçon, Practice, DebugLab, SqlLab, examens et dashboard ; exporter un exercice ; renseigner réflexion et tentative manuelle ; demander une exécution ; vérifier les prochaines actions et les limites ; contrôler ensuite historique, maîtrise et dashboard.

**Preuves attendues.** Navigation et apprentissage consultatif utilisables, indisponibilité clairement expliquée, export public seulement, aucune réussite automatique/tentative sérieuse/maîtrise créée par le runner manuel, aucune fausse preuve d'examen ou SQL.

**Arrêt propre au persona.** Export ou attestation manuelle compté comme validation, contenu privé exporté ou parcours sans action utile : P0/P1.

### P7 — Retour après deux semaines

**État initial.** Profil fictif ayant plan accepté, note/signets, une activité Practice, une carte personnelle et des cartes de récupération ; horloge initiale fixée puis avancée de quatorze jours.

**Hypothèses attaquées.** La reprise pourrait perdre l'état, culpabiliser, inventer une série, effacer la récence ou replanifier depuis l'ancienne échéance.

**Script.** Arrêter complètement l'application ; avancer l'horloge de quatorze jours dans le test déterministe ; redémarrer sur les mêmes données ; ouvrir dashboard, plan, leçon, Practice et Reviews ; répondre faux puis juste à des cartes ; vérifier prochaines échéances, maîtrise et preuves récentes.

**Preuves attendues.** État restauré, retard factuel sans dette ni pénalité, aucune série quotidienne, échec replanifié à J+1 depuis le jour réel, réussite au prochain intervalle documenté, preuve expirée/récence honnête et prochaine action lisible.

**Arrêt propre au persona.** Perte de données, culpabilisation structurante, date calculée depuis l'ancienne échéance ou maîtrise conservée malgré exigence de récence non satisfaite : P0/P1.

## Registre des preuves

### Vérifications réellement exécutées

| Preuve | Résultat |
|---|---|
| Règles adversariales unitaires : Mastery, Reviews, ExamIntegrity, Practice, diagnostic et plan | 57 réussies, 0 échec, 0 ignorée |
| Parcours HTTP E2E ciblés : diagnostic, Practice, mode manuel, Reviews, examen/dashboard | 7 réussis, 0 échec, 0 ignoré |
| Tests d'intégration de couverture des contenus S1–S24 et lecteur | 18 réussis, 0 échec, 0 ignoré |
| Validateur sur `content/reference` | code 0, 481 documents et 2 014 fichiers acceptés, 0 erreur |
| Validateur sur `content/sql` | code 0, 40 documents et 301 fichiers acceptés, 0 erreur |
| Recherche des prérequis non substitués dans les leçons | 69 leçons sur 70 contiennent littéralement `$previousLessonId` |
| Analyse des sections Markdown de leçon | 27 variantes de section sont répétées exactement au moins trois fois ; neuf sections identiques apparaissent 30 fois et neuf autres 29 fois |
| Recherche des exemples non matérialisés dans les exercices | 125 énoncés sur 135 contiennent littéralement `$(Convert-JsonCompact System.Object[] System.Object[][0])` ou la forme voisine pour la sortie |
| Inspection du parcours SQL public | la page annonce uniquement le dataset minimal ; `SqlLabService` utilise une attente et l'identité `sql-lab-reference-001` codées en dur ; aucune source de scénarios SQL pédagogiques n'existe dans le produit |
| Lancement navigateur sur données isolées | non exécuté : le démarrage d'un processus Web local a été refusé par l'environnement d'exécution avant création du processus |
| Rejeu Docker/SqlLab | non exécuté : l'accès au moteur Docker a été refusé par l'environnement d'exécution ; les preuves de l'incrément 11 n'ont pas été recyclées comme nouvel essai |

### Résultat par persona

| Persona | État | Preuves et conclusion |
|---|---|---|
| P1 — Débutant fragile | Partiel, non acceptable | Les tests diagnostic/plan confirment prudence, charge bornée et plan provisoire. Le parcours de lecture échoue néanmoins sur du contenu affichant un prérequis placeholder et des blocs génériques ; l'entretien UI intégral n'a pas pu être lancé. |
| P2 — Tricheur | Partiel, non acceptable | Les règles automatisées refusent quiz-only, preuves manuelles, répétition, faux examen et compensation. L'accès direct via navigateur n'a pas été rejoué. |
| P3 — Consommateur de solutions | Partiel, non acceptable | Les transitions et la source de révision existent, mais la page Practice affirme simultanément qu'aucune révision planifiée n'est calculée. L'interface donne donc un signal contradictoire au moment critique. |
| P4 — Faible SQL | Non exécutable | Le produit public ne permet pas de choisir et d'exécuter les 40 scénarios SQL/EF livrés ; il expose seulement le dataset technique de 06A. C'est un blocage produit P1, indépendamment de l'accès Docker refusé par l'environnement. |
| P5 — Fort quiz, faible pratique | Partiel, non acceptable | Les tests de maîtrise conservent les composantes absentes à zéro et les portes fermées. Les leçons et quiz massivement clonés empêchent toutefois de conclure que la compréhension mesurée porte sur des contenus distincts. |
| P6 — Sans Docker | Partiel, non acceptable | Les E2E prouvent l'absence de validation automatique en mode manuel. La page d'accueil Practice affirme à tort que Forge.NET ne compile ni ne teste jamais, alors qu'un runner Docker configuré existe ; l'essai navigateur complet n'a pas été possible. |
| P7 — Retour après deux semaines | Partiel, non acceptable | Les règles temporelles testées sont déterministes et sans pénalité. La reprise complète après arrêt/redémarrage n'a pas été exécutée dans l'interface isolée. |

Les sept personas ne sont donc pas déclarés exécutés intégralement. La condition d'acceptation correspondante échoue et la condition d'arrêt « persona non exécutable » s'applique.

## Défauts P0–P3

### P0

Aucun P0 distinct n'est déclaré avec une preuve suffisante. Cette absence ne compense pas les P1 systémiques.

### P1 ouverts

#### P1-01 — Soixante-neuf leçons sont des gabarits non finalisés

- **Reproduction :** 69/70 fichiers `lesson.md` affichent `$previousLessonId` comme prérequis.
- **Étendue :** des sections entières « Objectif », « Exercice guidé », « Exercice autonome », « Débogage », « Entretien », « Résumé », « Cartes » et « Test de maîtrise » sont identiques par groupes de 29, 30 et 10 leçons.
- **Exemples contradictoires :** la leçon SQL sur l'isolation réemploie un exercice générique de domaine de commandes et un breakpoint ; la défense finale en anglais réemploie le même exercice Azure simulé que les leçons cloud.
- **Impact :** le volume de 70 leçons est structurellement valide mais pédagogiquement artificiel. Un apprenant fragile reçoit un prérequis inexploitable et des activités ne permettant pas de travailler la notion annoncée.
- **Contradictions :** `AGENTS.md` et `CONTENT_GUIDE.md` interdisent les placeholders et exigent contenu autonome, exemple utile, pratique et test propres à l'activité.
- **État :** ouvert. La correction exige une réécriture et une revue humaine leçon par leçon ; elle ne peut pas être masquée par une substitution automatique du seul identifiant.

#### P1-02 — Cent vingt-cinq exercices publient un faux exemple PowerShell

- **Reproduction :** 125/135 `statement.md` contiennent une chaîne du type `$(Convert-JsonCompact System.Object[] System.Object[][0])` et la forme `[1]` pour la sortie.
- **Impact :** l'entrée/sortie n'est ni lisible ni exécutable par l'apprenant. Les exercices touchés couvrent algorithmique, S11–S24, Azure et autres domaines critiques ; seuls les dix exercices initiaux échappent au défaut.
- **Contradictions :** un placeholder est présenté comme fonctionnalité alors que les manifestes contiennent par ailleurs des exemples structurés valides.
- **État :** ouvert. Chaque exemple doit être matérialisé et relu contre la signature et les tests, sans remplacement textuel aveugle.

#### P1-03 — Les 40 scénarios SQL/EF ne sont pas intégrés au parcours d'apprentissage public

- **Reproduction :** `/sql-lab` annonce encore « aucun des douze scénarios pédagogiques » ; `SqlLabService` conserve une requête, une attente et l'identité `sql-lab-reference-001` fixes ; le gateway provisionne uniquement `SqlLabTemplate.SchemaAndDatasetSql`.
- **Impact :** le persona faible SQL ne peut ni choisir un scénario publié, ni lire son objectif, ni exécuter sa requête contre son dataset/attente, ni produire une observation liée à ce scénario hors examen. Les 40 dossiers validés restent du contenu serveur/test, pas un parcours utilisateur.
- **Contradictions :** la roadmap marque 06B et 08 complets et la documentation annonce 40 scénarios SQL/EF intégrés.
- **État :** ouvert. Une source de scénarios confinée, une session liée à identité/version/révision, le provisionnement du dataset, la validation privée et des E2E sont requis. Une simple modification du texte UI serait trompeuse.

#### P1-04 — Practice donne une instruction fausse après consultation d'une solution

- **Reproduction :** la page d'activité affiche « Aucun score de maîtrise ni révision planifiée n'est calculé » alors que `SqliteReviewSourceProvider` crée une source `SolutionViewed` et que la maîtrise relit les observations runner.
- **Impact :** le consommateur de solutions ne sait pas qu'une reprise est générée dans Reviews et reçoit deux explications incompatibles sur la conséquence de son acte.
- **État :** ouvert. Le message doit distinguer l'absence de maîtrise immédiate de la création d'une révision de récupération, avec une non-régression Web.

### P2 ouverts

#### P2-01 — La page d'accueil Practice décrit encore l'incrément 04A

Elle affirme que Forge.NET « n'exécute, ne compile et ne teste aucun code ». C'est vrai du processus Web et du mode manuel, mais faux d'une installation avec CodeRunner Docker. La page d'activité explique ensuite correctement les deux modes. Le texte et le test E2E qui l'impose doivent être alignés lors de la reprise.

#### P2-02 — Absence de panel humain indépendant

Aucun relecteur humain indépendant n'était disponible dans cette exécution. Cette limite ne crée pas le refus à elle seule, mais les explications personnelles, durées et ambiguïtés devront être échantillonnées par des apprenants réels après correction des P1 structurels.

### P3

Aucun P3 n'est priorisé : la correction cosmétique serait prématurée tant que les P1 restent ouverts.

## Corrections P0/P1 et non-régressions

Aucune correction produit n'a été appliquée. Le défaut P1-03 réclame une intégration verticale et P1-01/P1-02 une reprise éditoriale massive ; poursuivre par de petites corrections isolées aurait laissé croire que l'audit pouvait devenir favorable. La condition d'arrêt « contenu critique ambigu » impose le refus avant ce travail.

Le seul fichier créé par l'audit est ce rapport. Aucun test existant n'a été affaibli, aucun seuil n'a été modifié et aucune sécurité runner/SqlLab n'a été contournée.

## Reprise du 10 août 2026

Cette section est ajoutée après l'audit. **Elle ne modifie pas le verdict**, qui reste REFUSÉ : les
sept personas n'ont toujours pas été exécutés intégralement, et l'environnement de cette reprise ne
disposait ni de Docker ni de SQL Server.

### Ce qui est clos

| Défaut | État | Preuve |
|---|---|---|
| P1-02 — 125 énoncés d'exercice à faux exemple | **Clos** | `scripts/Repair-ContentExamples.ps1` reconstruit les exemples depuis `tests/visible/cases.json`. 0 marqueur résiduel sur 135 exercices et 40 scénarios SQL. |
| P1-03 — 40 scénarios SQL/EF hors du parcours | **Clos** | `ISqlScenarioSource`, `FileSystemSqlScenarioSource`, `SqlScenarioCatalog`, sélecteur dans `SqlLabPage`. Les 35 scénarios SQL sont choisissables ; les 5 scénarios EF restent au runner isolé. `SqlLabService` ne porte plus d'identité en dur. |
| P1-04 — instruction fausse après solution | **Clos** | `PracticePage` distingue l'absence de maîtrise immédiate de la carte de récupération réellement planifiée, avec non-régression E2E. |
| P2-01 — page d'accueil Practice datée | **Clos** | Le texte décrit le mode `CodeRunner` réellement configuré, lu depuis `CodeRunnerModeDescriptor`. |
| P1-01 — 69 leçons non finalisées | **Clos sur le contenu** | Les 70 leçons S1–S24 sont réécrites et sortent toutes du registre de dette. Le défaut résiduel `cloned-content` ne concerne plus aucune leçon. |

### La cause racine, et sa fermeture

Le défaut n'était pas éditorial mais mécanique. Trois générateurs échappaient par erreur leurs
sous-expressions PowerShell dans un here-string (`scripts/New-S1S10Content.ps1:114` et `:332`, et
leurs équivalents) : `$previousLessonId` et `$(Convert-JsonCompact …)` étaient écrits littéralement.
`Convert-JsonCompact` passait de plus son argument par le pipeline, ce qui aplatissait les tableaux.

Trois verrous ferment cette porte :

1. **Trois règles d'authenticité** dans le validateur — `unsubstituted-placeholder`,
   `cloned-content`, `hollow-lesson` — décrites dans `CONTENT_AUTHORING_STANDARD.md`. Elles portent
   sur le lot entier, ce qui est indispensable pour détecter la recopie.
2. **Un registre de dette à cliquet** (`content/authoring/content-debt.json`) : un défaut non
   déclaré refuse le lot, et une déclaration devenue inutile le refuse aussi. La dette ne peut donc
   que décroître, et `ContentAuthenticityTests` refuse tout dépassement du plafond figé.
3. **Des échafaudeurs non destructeurs** : ils n'écrasent plus aucun fichier existant sans `-Force`,
   et émettent des marqueurs `TODO:` que la première règle refuse. Un lot échafaudé mais non rédigé
   ne peut plus être publié comme terminé.

### Dette mesurée

| Relevé | Documents | Déclarations |
|---|---:|---:|
| Avant reprise | 376 | 667 |
| Après remise en état des exemples | 376 | 514 |
| Après reprise éditoriale S1–S4 | 364 | 480 |
| Après reprise éditoriale S5–S10 | 346 | 426 |
| Après reprise éditoriale S11–S13 | 337 | 399 |
| Après reprise éditoriale S14–S16 | 328 | 372 |
| Après reprise éditoriale S17–S19 | 319 | 342 |
| Après reprise éditoriale S20–S22 | 310 | 318 |
| Après reprise éditoriale S23–S24 | 306 | 306 |
| Après reprise des 135 échelles d'indices | 181 | 181 |
| Après reprise des 17 DebugLabs générés | 164 | 164 |
| Après reprise des briefs de projet | 159 | 159 |
| Après reprise des 28 scénarios SQL | 131 | 131 |
| Après reprise des 50 cartes d'anglais | 106 | 106 |
| Après reprise des 191 fiches d'entretien | **0** | **0** |

**La dette est éteinte.** Zéro `cloned-content`, zéro `hollow-lesson`, zéro
`unsubstituted-placeholder` : le registre ne déclare plus aucun document.

Le fait à retenir n'est pas le chiffre mais ce qu'il change. Tant qu'une dette subsistait, le cliquet
bornait un existant, et un défaut neuf pouvait passer sous le plafond sans être distingué d'un défaut
hérité. À zéro, le premier paragraphe recopié dans plus de trois documents d'un même lot fait échouer
le build, et aucune déclaration ne peut l'absorber.

La reprise finale a par ailleurs corrigé un défaut que le validateur ne voyait pas. Les trois règles
d'authenticité travaillent sur des paragraphes entiers ; une phrase gabarit insérée au milieu d'un
paragraphe par ailleurs propre y échappe. Or 50 réponses modèles d'entretien portaient la même phrase
de méthode, 73 autres la même phrase de preuve, et 25 réponses modèles d'anglais commençaient par un
collage grammaticalement cassé — « My decision is that On the stated window… ». Le comptage par
document ne mesurait donc pas la duplication réelle. Après reprise, les 197 fiches d'entretien
portent **401 critères observables distincts sur 401** et **204 erreurs fréquentes distinctes sur
204**, contre 50 et 29 auparavant ; les 51 cartes d'anglais portent 51 consignes, 51 réponses et 51
variantes distinctes, contre 3, 26 et 3.

Les vingt-quatre semaines sont désormais suivies sans ressource externe : soixante-dix leçons
rédigées, chacune avec ses quatorze sections, son quiz, entre trois et six blocs de code et un
contre-exemple montrant le code fautif puis sa correction. Elles sont adossées à 135 exercices C#
prouvés corrects hors Docker, 25 DebugLabs, 40 scénarios SQL et 8 examens.

La boucle d'aide a été reprise ensuite. Treize formulations d'indice de niveau 4 couvraient les 135
exercices — une seule pour soixante-quinze d'entre eux — alors que ce niveau est la dernière marche
avant un déverrouillage de solution qui met la pratique à zéro. Les 135 indices de niveau 4, les 135
jeux d'erreurs fréquentes et les 135 explications sont désormais propres à leur exercice, écrits
après lecture de sa solution et de ses cas cachés. `ExerciseHintQualityTests` fige cet état :
indices distincts par exercice, aucun texte partagé par plus de trois exercices, aucun indice
recopiant une ligne de solution.

Le même défaut existait un cran plus haut, sur la compétence la plus transférable du parcours.
Dix-sept des vingt-cinq DebugLabs nommaient la cause dans le ticket — « La division utilise une
longueur nulle » —, portaient un journal réduit à une ligne sans mesure, et partageaient une grille
d'évaluation dont les termes attendus étaient « borne, condition, mutation ». Les leçons de la
semaine 7 enseignent une méthode en quatre temps ; ce matériel court-circuitait les trois premiers et
l'évaluation ne pouvait pas voir la différence. Les dix-sept scénarios ont été repris : ticket de
symptôme, journal portant des mesures réelles, comportement attendu contractuel, gestes et questions
d'observation propres au défaut, note de non-régression corrigée et grille nommant les concepts
réellement en jeu. `broken/`, `correction/` et les cas de test sont restés inchangés à l'octet près,
vérifié par empreinte. `DebugScenarioQualityTests` fige l'état atteint par quatre règles qui
échouaient toutes sur exactement ces dix-sept scénarios.

### La rétention espacée n'avait qu'une source, et elle expirait

La composante « rétention espacée » pèse 15 % du score et n'était alimentable que par le bilan
d'entrée : trente-six questions, passées une fois, dont les preuves sont écartées passé la durée de
validité. Le poids d'une composante sans preuve n'étant jamais redistribué, un domaine critique
plafonnait alors à exactement 85 — le seuil lui-même, atteignable seulement avec 100 partout
ailleurs et sans le moindre indice — et un profil sérieux mais imparfait tombait à 79,25. Les portes
A→D étaient donc infranchissables passé la durée de validité, pour tout le monde.

Les cartes de révision des exercices sont devenues une source admissible, à garanties inchangées :
correction serveur, mode à choix, carte déclarant pouvoir produire une preuve, et apparition
conditionnée à une soumission réelle au runner. **Aucun poids, aucun seuil, aucun plafond d'aide n'a
bougé** ; `MasteryRulesTests` documente le blocage à 79,25 et prouve son ouverture à 94,25 par la
seule addition d'une preuve.

Restait l'effet, distinct de la mécanique : la banque pilote couvrait 42 exercices sur 135, et le
domaine C# — celui que la porte A exige à 85 — n'en comptait qu'un sur soixante-quatorze. La banque
compte désormais **230 cartes sur 115 exercices** : les quatre domaines critiques dont la pratique
passe par des exercices — C#, débogage, API, tests — sont couverts intégralement. Deux règles de
`ReviewCardQualityTests` le figent : tout exercice d'un domaine critique porte ses cartes, et le
domaine déclaré par une carte est celui déduit de la première compétence de son exercice. La
première échouait sur 109 exercices avant la banque, sur 73 après la banque pilote, sur aucun
aujourd'hui.

Le domaine SQL, cinquième domaine critique, reste hors de portée : aucun exercice ne porte de
compétence `sql.*`, sa pratique passant par des scénarios de laboratoire. Le couvrir demande une clé
de carte généralisée de l'exercice vers l'élément pratiqué. La porte A n'en dépend pas.

### Le verdict de maîtrise ne se prononçait jamais

Le score pouvait monter ; le verdict, lui, ne basculait pas. `MasteryPolicyCatalog` déclare
**vingt-trois clés d'accomplissement** et une seule avait un producteur — `exam.90-minutes`. La
porte A exige « mini-projet console vérifié » : rien dans le produit ne pouvait satisfaire cette
exigence, et les portes B, C et D s'enchaînant derrière elle, **les quatre étaient fermées
définitivement**. L'apprenant lisait « Porte A — bloquée » le dernier jour comme le premier.

Les neuf projets existaient pourtant en contenu, mais sans starter, sans suite exécutable et **sans
page** : le produit les validait au chargement et ne les montrait jamais.

Les quatre projets console — S2, S4, S5, S7 — portent maintenant un brief au contrat précis, un
starter, un corrigé de référence et **une suite d'acceptation par jalon**, exécutée par le même bac
à sable que les exercices : aucun quota, aucune image, aucune règle d'isolement n'a été touchée. Une
soumission dont toutes les suites passent produit `project.console` en `AutomaticTests` ; une
soumission faite en mode manuel est enregistrée comme déclarée et ne prouve rien.

`ProjectCorrectnessTests` vérifie hors Docker que chaque corrigé de référence passe ses cas et que
chaque starter en échoue au moins un. `MasteryRulesTests` prouve le passage de « porte A bloquée au
seul motif du mini-projet » à « porte A ouverte », sans qu'aucun seuil ne bouge.
`ProjectAchievementTests` tient l'inventaire des **dix-neuf** clés encore sans producteur — un
plafond qui ne peut que descendre.

Deux clés en sont sorties depuis, toutes deux au-delà de la porte A : `code-review`
(`project-code-review-001`, S31) et `ef-core` (`project-orders-database-001`, dont les trois suites
exécutent du vrai EF Core contre une vraie base SQLite dans le bac à sable). Cinq autres clés —
`api.functional`, `tests.unit`, `tests.integration`, `docker`, `ci` — ont été examinées lors du même
travail et **refusées** : le bac à sable ne sert aucune requête HTTP, ne découvre aucun test écrit par
l'apprenant et ne bâtit aucune image. Leur produire un accomplissement sur un exercice qui *raisonne
sur* le sujet aurait fait descendre le compteur sans rien ouvrir. Le détail par clé est dans
`docs/MASTERY.md` et dans l'inventaire lui-même.

### Le score ne pouvait pas atteindre sa propre barre

Le producteur d'accomplissement ci-dessus était correct, mais le rapport a un temps affirmé que la
porte A devenait « franchissable par le travail ». **C'était faux.** Un accomplissement produit ne
dit rien du plafond d'un score, et c'est là que se trouvait le vrai blocage.

Deux composantes sur cinq n'avaient aucun producteur — explication (10 %) et quiz (5 %) — et la
pratique comme les examens attribuaient toute observation à un domaine codé en dur : un exercice
`api-*` alimentait le score C#, jamais le score Api. Le poids d'une composante sans preuve n'étant
jamais redistribué, les plafonds mesurés étaient : débogage **60** pour un seuil de 80, SQL **70**
pour 75, Api et Tests **15** pour 85, cinq domaines à **0** pour 80. La porte A exigeant
débogage ≥ 80 et SQL ≥ 75, **elle était mathématiquement impossible**.

Trois corrections, aucun poids ni seuil touché : `MasterySkillDomains` devient la source unique du
couple compétence → domaine et sert la pratique comme les examens ; le quiz de leçon, déjà corrigé
côté serveur, produit enfin une observation ; la banque de cartes passe à **350 cartes sur 175
éléments** — les 135 exercices et les 40 scénarios SQL, dont la clé de carte a été généralisée à
l'élément pratiqué. Tous les domaines plafonnent désormais à **90**.

`MasteryReachabilityTests` calcule ce plafond à partir d'un inventaire des producteurs et refuse
qu'un domaine plafonne **à** son seuil ou en dessous : un plafond égal au seuil n'est franchi qu'avec
cent sur chaque composante, ce qui est un blocage déguisé en objectif. Écrit avant les corrections,
il échouait sur six domaines ; il est vert.

### Ce qui reste ouvert

- **L'explication**, 10 % du score : aucune preuve serveur honnête n'existe pour cette composante.
  Son poids est perdu, ce qui fixe le plafond général à 90 — au-dessus des deux seuils (80 et 85),
  donc coûteux en score sans fermer ni domaine ni porte, et dit plutôt que tu.

  La reprise a cherché puis refusé trois routes : la carte à choix sur le « pourquoi » d'une solution
  (elle mesure la reconnaissance, l'acte que le quiz mesure déjà, et paierait deux fois le même geste),
  l'explication personnelle du protocole de pratique (contrôlée en longueur et en non-recopie, donc sur
  l'effort et non la justesse, et atteignable seulement après une solution consultée, c'est-à-dire sur
  un exercice contaminé), et la rubrique déterministe décrite dans `CONTENT_GUIDE.md` (elle note une
  transcription si ses concepts obligatoires sont publiés, une devinette s'ils sont secrets). Le geste
  que la composante nomme — produire un compte rendu causal dans ses propres mots — n'a pas de
  substitut machine : ce qui le juge est un lecteur. L'explication rejoint donc en nature les six
  exigences « jugement humain », dont elle est la seule **composante**. `MasteryRulesTests` rend le
  refus exécutable ; le raisonnement est dans `docs/MASTERY.md`.
- **Les portes B, C et D** : dix-neuf de leurs exigences n'ont aucun producteur. Elles sont
  désormais visibles, comptées **et diagnostiquées**, mais toujours pas satisfaites.

  L'inventaire de `ProjectAchievementTests` porte maintenant, pour chaque clé, ce qui lui manque :
  **treize** attendent un livrable vérifié côté serveur, **six** exigent un jugement humain et ne
  descendront jamais par du code — Git propre, présentation, entretien blanc, architecture pragmatique,
  anglais, défense finale. Deux clés sont sorties de l'inventaire, `code-review` et `ef-core`, et cinq
  autres ont été examinées puis refusées faute de preuve honnête possible dans le bac à sable.

  La reprise a cherché quelle clé pouvait être branchée sur une preuve **déjà collectée**. Réponse
  mesurée : **aucune**. Le candidat le plus crédible, « EF Core », a été instrumenté puis retiré après
  vérification : `FileSystemSqlScenarioSource` n'expose que le mode de contrat `sql`, or les cinq
  scénarios `ef-*` déclarent le mode `ef` et sont donc absents du laboratoire ; et un scénario EF n'est
  tirable en examen que s'il porte un dossier `exam/`, que **trois des cinq n'ont pas**. Aucun chemin du
  produit ne permet de valider les cinq. Livrer ce producteur aurait fait descendre le plafond de clés
  sans producteur sans rien débloquer — le faux signal exact que ce cliquet existe pour empêcher.
  `EfScenarioReachabilityTests` fige le diagnostic.

  La clé a été produite plus tard, par une preuve **nouvelle** et non par celles-là : un projet
  vérifiable dont les suites exécutent EF Core dans le bac à sable. Les deux constats coexistent sans se
  contredire — le produit savait exécuter EF Core, il n'avait simplement aucun chemin vérifié qui y
  menât.

  Fait à retenir pour l'audit lui-même : trois scénarios de contenu publiés, validés par le validateur
  et comptés dans les volumes sont **inatteignables par tout chemin d'apprenant**. Le validateur
  contrôle la structure d'un document, jamais son accessibilité depuis le produit. C'est le même angle
  mort que celui relevé sur les laboratoires.
- Les domaines non critiques encore découverts, dont le seuil de 80 reste franchissable sans
  rétention : intégration continue, architecture, Docker et anglais, soit 20 exercices.
- Volume de pratique en S11–S24 : **5,4** exercices par semaine contre 8,8 en S1–S10, après le lot 1
  de la reprise décrite dans `ROADMAP.md`, le lot JWT de S14, le lot OAuth/OIDC de S21 et le lot
  REST de S11–S13. Les semaines S11 à S14 atteignent désormais la cible de huit — S11 à S13 à dix
  exercices depuis le lot REST, S14 à douze depuis le lot JWT ; restent S15 à S17, encore à six.
  S21 monte à sept exercices. La reprise des échelles d'indices, elle, améliorait la qualité de la
  boucle et non son volume.
- Manques REST comblés (T10) : versionnage, ETag et concurrence conditionnelle, limitation de débit,
  Cache-Control, CORS et webhooks ont chacun leur leçon et deux exercices en S11–S13. Le lien entre
  l'ETag conditionnel et la concurrence optimiste des bases — `sql-isolation-001`,
  `ef-core-data-access-001` — est rendu explicite, et les webhooks réutilisent la vérification de
  signature HMAC du lot jetons. Restent hors périmètre les fondamentaux distribués (T11) et le
  front-end (T12).
- Forme des activités S19–S22 : les exercices `docker-*`, `ci-*` et `azure-*` sont des fonctions
  pures sur un domaine d'entrée de quelques valeurs. Ils entraînent la décision et non le geste, et un
  domaine aussi étroit se résout par une table de correspondance mémorisée. La pratique réelle de ces
  sujets passe par les huit laboratoires de `content/labs/`, longtemps invisibles du parcours et
  désormais servis par les pages `/labs` : chaque manifeste `lab.json` est validé au démarrage, chaque
  page annonce que la réussite est déclarée par l'apprenant, hors du bac à sable, et ne produit aucune
  preuve de maîtrise. Le rattachement rend les laboratoires trouvables ; il ne change pas la forme des
  exercices, qui reste le défaut relevé ici.
- ~~Dédoublonnage des 190 fiches d'entretien — 190 questions distinctes mais 29 critères observables
  seulement — et des 50 cartes d'anglais.~~ **Clos** : 401 critères et 204 erreurs fréquentes
  distincts sur les 197 fiches, 51 consignes et 51 réponses distinctes sur les cartes d'anglais, et
  registre de dette à zéro. Ce qui reste ouvert sur ces fiches n'est plus la duplication mais leur
  nombre de critères — deux ou trois par fiche —, dont l'augmentation demanderait un panel humain
  pour juger de leur pertinence.
- **P2-02** : aucun panel humain indépendant n'a été réuni.
- Les sept personas n'ont pas été rejoués. Docker est désormais disponible — voir la reprise du
  17 août 2026 — mais aucun pilotage de navigateur n'existe dans le dépôt, et les tests de bout en
  bout s'exécutent en processus sur `WebApplicationFactory` sans circuit Blazor interactif. La
  condition porte sur des trajets d'interface qu'aucun de ces tests ne traverse.
- Les tests d'intégration dépendant de Docker **s'exécutent désormais** : les 76 échecs
  environnementaux sont tombés à zéro, et la suite complète est verte. Cette exécution a révélé trois
  défauts bloquants qu'aucun test hors Docker ne pouvait voir ; ils sont corrigés et couverts.
- **Le trajet réel d'une soumission de projet est désormais exécuté** de la soumission au verdict,
  dans un conteneur isolé, par `ProjectSubmissionDockerRunnerTests`. Il l'était d'autant moins
  auparavant qu'il ne fonctionnait pas : deux défauts l'empêchaient d'aboutir. Restent hors de portée
  d'un test l'éditeur du navigateur et l'affichage de l'accomplissement dans le tableau de
  progression.
- 43 des 142 exercices ne figurent dans aucune banque d'examen : ils ne peuvent jamais être tirés. Ce
  nombre n'a pas monté malgré sept exercices neufs, tous inscrits dans la banque de leur examen ; il
  n'a pas baissé non plus, le reliquat portant sur des familles antérieures à la reprise.

## Reprise du 17 août 2026 — avec un démon Docker

Un moteur Docker était disponible pour la première fois. Ce qui a été exécuté, dans l'ordre :
`scripts/start-sql-lab.ps1` (conteneur SqlLab et pont de test rendus sains, aucun secret affiché),
construction de l'image runner depuis `src/ForgeDotNet.CodeRunner/Container`, puis la suite
d'intégration complète.

**Les 76 échecs environnementaux sont tombés à zéro : 637 tests d'intégration verts en une seule
exécution**, dix-neuf de plus qu'avant la reprise — deux qui parcourent le trajet de soumission dans
un conteneur réel, dix-sept qui figent hors Docker la convention de nommage des suites. Mais le
résultat utile de cette reprise n'est pas ce chiffre : c'est ce que l'exécution réelle a révélé, et
qu'aucune quantité de tests hors Docker n'aurait montré.

### Trois défauts, tous dans une couture

Chacun se tenait entre deux composants séparément prouvés corrects. C'est le constat le plus important
de cette reprise, et il porte sur la stratégie de test elle-même, pas sur une ligne de code.

**P1-A — Aucun conteneur ne pouvait être créé.** Le runner passait à `docker create` l'option de
montage `bind-nonrecursive`, dépréciée depuis Docker 25 et **supprimée dans Docker 29**. Le moteur
refusait la création ; le produit rendait « Docker a refusé la politique d'isolation ». Sur un poste à
jour, **aucun exercice, aucun DebugLab, aucun projet ne pouvait s'exécuter** — et la vérification hors
ligne restait verte pendant ce temps. Corrigé par `bind-recursive=disabled`, dont l'inspection confirme
qu'elle produit la même option (`BindOptions.NonRecursive = true`). Les dix-huit contrôles d'isolation
de `DockerCodeRunnerSecurityTests` passent, dont celui qui vérifie chaque garantie effective : aucune
protection n'a été échangée contre ce correctif.

**P1-B — Aucune soumission de projet ne pouvait aboutir.** `SubmitProject` construit une cible
d'exécution de la forme `<projet>.<jalon>` — c'est la forme que `FileSystemProjectSource.FindSuiteAsync`
redécoupe. Mais le contrat d'exécution validait cette cible contre un motif qui **n'admettait pas le
point**, hérité de l'époque où seuls des exercices s'exécutaient. La requête était donc rejetée avant
tout appel au bac à sable. Conséquence : le producteur d'accomplissement de la porte A n'avait jamais
pu se déclencher, et les clés `project.console`, `code-review` et `ef-core` étaient inatteignables en
pratique alors que tout le disait produit. Le motif admet désormais un second segment, et un seul,
sans point interne : « .. » reste impossible par construction.

**P1-C — Deux projets sur six nommaient mal leur manifeste de suite.**
`project-code-review-001` et `project-orders-database-001` déclaraient `exerciseId` d'après le seul
identifiant de projet, là où les quatre projets console déclarent la cible complète. Leurs suites
étaient franchissables sous `ProjectCorrectnessTests` et introuvables à la soumission. Corrigé sur les
cinq manifestes concernés.

### Ce que ces trois défauts apprennent

`ProjectCorrectnessTests` prouvait que chaque suite est franchissable.
`DockerCodeRunnerSecurityTests` prouvait que le bac à sable tient sa politique. Les deux étaient verts,
et **le produit était inutilisable** : rien ne prouvait qu'une soumission traverse effectivement le bac
à sable. Une preuve par morceaux ne se recompose pas d'elle-même en preuve de bout en bout, et l'écart
entre les deux est exactement l'endroit où un défaut peut vivre indéfiniment sans être vu.

Deux règles nouvelles ferment cette couture :
`ProjectSubmissionDockerRunnerTests` exécute le trajet réel — corrigé de référence, conteneur isolé,
trois suites, statut agrégé, plus le cas symétrique du squelette qui doit échouer ; et
`SuiteManifestIsNamedAfterTheRunIdentifierTheProductWillEmit` vérifie **hors Docker**, pour chaque suite
publiée, que son manifeste nomme la cible que le produit émettra et que le contrat l'accepte. La
seconde aurait suffi à attraper P1-B et P1-C en une seconde d'exécution.

### Ce qui n'a pas été fait, et pourquoi le verdict ne bouge pas

**Les sept personas n'ont pas été rejoués.** La condition n'est pas satisfaite : il n'existe dans ce
dépôt aucun pilotage de navigateur. Les 61 tests de bout en bout s'exécutent sur
`WebApplicationFactory`, c'est-à-dire un client HTTP en processus — ils ne chargent aucun circuit
Blazor interactif, ne cliquent rien, et ne peuvent donc pas tenir lieu de rejeu. Les personas portent
précisément sur des trajets d'interface : accès prématuré à une solution, message après consultation,
choix d'un scénario SQL, retour après quatorze jours.

**Le trajet de soumission est vérifié du rendu au verdict, pas de la frappe au pixel.** L'éditeur du
navigateur et l'affichage de l'accomplissement dans le tableau de progression restent hors de portée
d'un test. Le dire est plus utile que de laisser croire le contraire.

**Aucun panel humain n'a été réuni.** Cette condition ne dépend pas de l'outillage.

**Le verdict reste donc REFUSÉ**, et la reprise le confirme au lieu de le lever. Elle apporte
néanmoins ce qu'un rejeu devait apporter : trois défauts bloquants trouvés, corrigés et couverts, dont
deux rendaient le produit inutilisable pour n'importe quel apprenant sur un poste à jour.

## Auto-suffisance de l'application, examinée séparément du contenu

Les reprises précédentes portaient sur ce que le parcours enseigne. Celle-ci pose une autre question :
un lecteur qui clone le dépôt et suit la documentation obtient-il une application qui sert ce qu'elle
embarque ? **Non**, pour deux raisons indépendantes du contenu, toutes deux fermées depuis.

### L'application livrée ne validait rien, et ne le disait pas

Le mode par défaut est `Manual` dans `appsettings.json` **et** codé en dur dans `docker-compose.yml`.
En mode manuel, `UnavailableCodeRunner` rend un résultat sans aucun test exécuté ; or la politique
n'admet une preuve que si des tests ont réellement été rapportés. La chaîne complète était donc :
aucune observation vérifiée, aucun domaine validable, **porte A fermée par configuration** et non par
manque de travail. La page de maîtrise affichait « fermée » sans distinguer les deux.

S'y ajoutait un obstacle matériel : la construction de l'image du bac à sable **n'était documentée
nulle part**, et son contexte n'est pas la racine du dépôt mais `src/ForgeDotNet.CodeRunner/Container`
— ce qu'aucun document ne permettait de deviner. Le message d'erreur de démarrage décrivait le format
attendu (`sha256:` complet) sans dire comment l'obtenir.

Fermé par : `scripts/build-code-runner.ps1`, qui construit le bon contexte et rend la référence
immuable à configurer ; un message de démarrage qui nomme ce script ; une section « Valider des
exercices » dans le README ; une bannière sur `/mastery` qui dit, en mode manuel, que l'installation ne
peut produire aucune preuve — *« les scores ci-dessous mesurent une installation, pas votre niveau »*.
`InstallationHonestyTests` et un test de bout en bout figent l'ensemble.

Le mode Compose **reste** non validant, et c'est délibéré : exécuter du code soumis exigerait de monter
le socket Docker de l'hôte dans le conteneur web, c'est-à-dire d'y ouvrir un chemin d'évasion, à
l'opposé du modèle de menace. Ce qui change est que le fichier et le README le disent, au lieu de
laisser le lecteur le découvrir.

### 293 documents sur 618 n'avaient aucun écran

`ContentDocumentType` déclare dix familles. Huit avaient une route. Deux n'en avaient aucune :
**242 fiches d'entretien** et **51 cartes d'anglais**. Chargées, validées par `--validate-content`,
comptées dans les instantanés de volume — et illisibles. Aucun fichier de `ForgeDotNet.Application` ni
de `ForgeDotNet.Web` ne les mentionnait : elles n'existaient que dans la couche de chargement.

C'est l'angle mort déjà nommé plus haut à propos des scénarios EF, mais d'une tout autre ampleur : *le
validateur contrôle la structure d'un document, jamais son accessibilité depuis le produit*. Un lot de
contenu pouvait donc être publié, mesuré et compté sans jamais atteindre personne.

Ces deux familles portent précisément ce qui prépare aux exigences d'entretien et d'anglais que
`docs/HUMAN_REVIEW.md` confie à un relecteur humain. Le matériel était soigné — `observableCriteria`,
`modelAnswer`, `commonMistakes` — et inatteignable.

Fermé par : `/interviews`, `/interviews/{id}`, `/english`, `/english/{id}`, un lien depuis chacun des
175 exercices dont une fiche prolonge le travail, et la mention explicite qu'aucune de ces pages ne
produit de preuve de maîtrise. La réponse modèle reste masquée jusqu'à une révélation demandée : la
livrer d'emblée transformerait la préparation en lecture.

**La règle qui généralise** : `ContentReachabilityWebTests` exige, pour **chaque** valeur de
`ContentDocumentType`, une route déclarée et réellement servie. Un type ajouté sans écran fait échouer
le test au moment où la question se pose utilement. Cette règle aurait signalé les 293 documents dès
leur publication.

### Ce que cela ne change pas

Le verdict. Ces deux chantiers rendent l'application auto-suffisante **techniquement** — utilisable en
suivant sa propre documentation, et servant l'intégralité du contenu qu'elle embarque. Ils ne changent
ni poids, ni seuil, ni condition de porte, et n'ajoutent aucun producteur d'accomplissement. Les portes
B, C et D restent fermées, les personas restent non rejoués, le panel humain reste à réunir.

## Réaudit

Aucun réaudit favorable n'est possible tant que les quatre P1 restent ouverts. La reprise devra au minimum :

1. ajouter des tests de contenu qui refusent les marqueurs non substitués et les exemples générés illisibles — *fait : trois règles d'authenticité et un registre à cliquet* ;
2. revoir les 69 leçons et 125 énoncés contre leurs objectifs, signatures et tests — *fait : 70 leçons réécrites, 135 exercices prouvés corrects hors Docker* ;
3. intégrer les scénarios SQL/EF au parcours utilisateur avec isolation inchangée ;
4. corriger les messages Practice et leurs tests E2E ;
5. exécuter les sept scripts sur données séparées, avec navigateur et Docker disponibles ;
6. rejouer `scripts/verify.ps1`, le validateur complet et les contrôles Git.

## Verdict

**REFUSÉ.**

Le refus repose sur quatre P1 ouverts, dont deux défauts de contenu systémiques, un parcours SQL non intégré et un signal contradictoire après solution. Les sept personas n'ont pas été exécutés intégralement et le persona faible SQL est bloqué par le produit. Les validateurs et 82 tests ciblés sont verts, mais ils démontrent précisément un angle mort : ils acceptent la structure et l'exécution sans refuser les placeholders visibles ni la duplication pédagogique massive.

L'incrément 12 doit rester non coché. Aucun incrément suivant n'est créé implicitement.

**Le verdict reste REFUSÉ après la reprise éditoriale.** Les défauts de contenu qui le motivaient
sont clos et mesurés — 70 leçons rédigées, plus aucune leçon dans le registre de dette, 135 exercices
prouvés corrects — mais les deux conditions de levée sont inchangées : les sept personas n'ont pas
été rejoués, et ni navigateur ni démon Docker n'étaient disponibles pour le faire. Un verdict
favorable ne peut être prononcé que par cette exécution, pas par la lecture des tests qui, eux, ne
peuvent pas s'exécuter.

**Le verdict reste REFUSÉ après le rejeu avec un démon Docker.** La première des deux conditions est
partiellement remplie et partiellement non : la suite d'intégration s'exécute intégralement, mais les
sept personas n'ont pas été rejoués faute de tout pilotage de navigateur — les tests de bout en bout
sont des appels HTTP en processus, non un navigateur qui clique. La seconde condition, le panel
humain, n'a pas bougé. Ce rejeu a en revanche produit ce qu'un rejeu doit produire : trois défauts
bloquants, dont deux rendaient le produit inutilisable sur un poste Docker à jour, trouvés parce que
le code s'est enfin exécuté au lieu d'être seulement vérifié.

**Le verdict reste REFUSÉ après l'extinction de la dette éditoriale.** Le registre est passé de 159 à
zéro par la reprise des 28 scénarios SQL, des 50 cartes d'anglais et des 191 fiches d'entretien, et le
plafond de `ContentAuthenticityTests` est descendu à zéro en conséquence. C'est le dernier défaut de
contenu systémique qui se ferme, et cela ne déplace pas le verdict d'un cran : les deux conditions de
levée n'ont pas bougé. Il faut noter au contraire ce que cette reprise apprend sur l'audit lui-même —
la duplication réelle était plus large que ce que le registre comptait, parce que les règles portent
sur des paragraphes et non sur des phrases. Un chiffre à zéro mesure donc l'absence de défaut
détectable par ces trois règles, pas l'absence de contenu générique. Seul un panel humain peut
prononcer la seconde.

## Backlog P2/P3 et risques résiduels

Ordre de reprise recommandé, sans constituer une nouvelle roadmap :

1. **P1-01/P1-02 — contenu :** inventaire par semaine, réécriture en lots révisables, exemples réels, prérequis exacts, revue humaine et test anti-placeholder.
2. **P1-03 — SQL/EF :** conception ciblée à valider humainement si elle modifie le contrat de session ; conserver les frontières 06A et les tests cachés privés.
3. **P1-04/P2-01 — messages Practice :** corriger après les règles, afin que l'UI décrive le comportement réellement livré.
4. **P2-02 — échantillon humain :** au moins débutant fragile, développeur fort en quiz et profil faible SQL, avec mesure du temps et reformulation sans aide.
5. **Risque externe à rejouer :** accès Docker et lancement navigateur n'ont pas été autorisés dans l'environnement de cet audit ; aucune conclusion nouvelle n'est tirée sur leur exécution effective.

Le jury maintient une posture indépendante et contradictoire. Aucun résultat n'est formulé comme certification, promesse d'emploi ou promesse salariale.
