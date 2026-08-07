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

## Réaudit

Aucun réaudit favorable n'est possible tant que les quatre P1 restent ouverts. La reprise devra au minimum :

1. ajouter des tests de contenu qui refusent les marqueurs non substitués et les exemples générés illisibles ;
2. revoir les 69 leçons et 125 énoncés contre leurs objectifs, signatures et tests ;
3. intégrer les scénarios SQL/EF au parcours utilisateur avec isolation inchangée ;
4. corriger les messages Practice et leurs tests E2E ;
5. exécuter les sept scripts sur données séparées, avec navigateur et Docker disponibles ;
6. rejouer `scripts/verify.ps1`, le validateur complet et les contrôles Git.

## Verdict

**REFUSÉ.**

Le refus repose sur quatre P1 ouverts, dont deux défauts de contenu systémiques, un parcours SQL non intégré et un signal contradictoire après solution. Les sept personas n'ont pas été exécutés intégralement et le persona faible SQL est bloqué par le produit. Les validateurs et 82 tests ciblés sont verts, mais ils démontrent précisément un angle mort : ils acceptent la structure et l'exécution sans refuser les placeholders visibles ni la duplication pédagogique massive.

L'incrément 12 doit rester non coché. Aucun incrément suivant n'est créé implicitement.

## Backlog P2/P3 et risques résiduels

Ordre de reprise recommandé, sans constituer une nouvelle roadmap :

1. **P1-01/P1-02 — contenu :** inventaire par semaine, réécriture en lots révisables, exemples réels, prérequis exacts, revue humaine et test anti-placeholder.
2. **P1-03 — SQL/EF :** conception ciblée à valider humainement si elle modifie le contrat de session ; conserver les frontières 06A et les tests cachés privés.
3. **P1-04/P2-01 — messages Practice :** corriger après les règles, afin que l'UI décrive le comportement réellement livré.
4. **P2-02 — échantillon humain :** au moins débutant fragile, développeur fort en quiz et profil faible SQL, avec mesure du temps et reformulation sans aide.
5. **Risque externe à rejouer :** accès Docker et lancement navigateur n'ont pas été autorisés dans l'environnement de cet audit ; aucune conclusion nouvelle n'est tirée sur leur exécution effective.

Le jury maintient une posture indépendante et contradictoire. Aucun résultat n'est formulé comme certification, promesse d'emploi ou promesse salariale.
