# Sécurité

## Contrôles DebugLab 05

DebugLab réemploie exclusivement le runner Docker durci de 04C. La source privée vérifie l'identité, la version et la révision de chaque scénario, confine tous les fichiers sous `content/`, refuse traversal et points de réanalyse, borne les tailles et impose UTF-8 strict. Les logs initiaux sont refusés s'ils contiennent un chemin hôte/interne, un jeton ou un marqueur de donnée sensible.

Avant correction, Domain exige une hypothèse, des preuves et les observations Breakpoint, Watch, Locals et Call Stack. Avant exécution, la correction et un test de non-régression doivent être décrits. Le code soumis n'est jamais persisté : seule une empreinte SHA-256 et le résultat borné sont conservés. Les cas cachés et la grille de cause racine restent côté serveur. La solution n'entre dans la projection Web qu'après deux corrections échouées et une demande explicite ; cette transition reste non terminée et n'attribue aucune maîtrise.

La preuve automatisée couvre l'accès prématuré à la solution, l'absence de code source en SQLite/export, l'expurgation des cas cachés et seize exécutions Docker (huit versions cassées puis huit corrigées). Les limites et le parcours manuel sont détaillés dans `docs/DEBUGLAB.md`.

## Modèle de menace

Actifs : machine hôte, fichiers utilisateur, secrets/environnement, progression, solutions/tests cachés, disponibilité locale et intégrité des scores. Entrées hostiles : code C#, SQL, Markdown, archives importées, noms de fichiers et données restaurées. L'utilisateur local est légitime mais peut soumettre du code accidentellement ou volontairement dangereux.

Docker réduit le risque mais n'est pas une frontière parfaite. Forge.NET ne doit pas être exposé directement à Internet et ne doit pas exécuter de runner privilégié.

## CodeRunner — contrôles obligatoires

- Jamais d'exécution dans le processus web.
- Image minimale épinglée par digest, reconstruite et analysée régulièrement.
- Utilisateur non-root, capabilities supprimées, `no-new-privileges`, profil seccomp/AppArmor lorsque disponible.
- Réseau désactivé ; aucun port publié.
- Racine en lecture seule, espace de travail/tmpfs jetable, aucun montage hôte hors répertoire temporaire préparé.
- Docker socket, devices, SSH agent et répertoires utilisateur jamais montés.
- Liste blanche de commandes et arguments construits sans shell ; aucun concaténage de commande utilisateur.
- CPU, mémoire, PIDs, taille disque/fichier, temps et sortie bornés ; concurrence globale limitée.
- Processus et conteneur tués puis supprimés après succès, erreur, annulation ou crash.
- Environnement minimal sans secret ; identifiants aléatoires non prédictibles.
- Résultats normalisés ; code et sortie tronquée non journalisés par défaut.

Tests d'abus : boucle infinie, allocation mémoire, fork/process bomb, output bomb, accès réseau, traversal, lecture hôte, variable d'environnement, subprocess interdit et conteneur orphelin.

## Revue renforcée CodeRunner 04C

### Frontières et menaces

Le code soumis, ses noms de fichiers et sa sortie sont hostiles. Les actifs à protéger sont l'hôte Windows, le moteur Docker, les fichiers et secrets locaux, la disponibilité du poste, les tests cachés et l'intégrité des résultats. Les frontières sont : navigateur vers Web, Web vers le port `ICodeRunner`, adaptateur vers le client Docker, montage `/input` vers le conteneur, processus hôte du runner vers le compilateur, puis compilateur vers le processus de tests.

Les scénarios revus sont l'exécution de commandes arbitraires, l'évasion de conteneur, l'accès réseau, la lecture/écriture hôte, l'héritage de secrets, le fork bomb, l'épuisement CPU/mémoire/disque/sortie, le traversal, la falsification du protocole de résultat, la fuite des tests cachés, l'abandon d'artefacts et le contournement par configuration. Docker réduit ces risques sans promettre une isolation absolue.

### Contrôles effectifs

- L'image officielle `mcr.microsoft.com/dotnet/sdk:10.0-alpine` est figée par digest dans les deux étapes du Dockerfile. L'adaptateur refuse un tag et lance uniquement un ID local `sha256` complet dont il inspecte l'utilisateur, le point d'entrée, les labels, l'OS, l'architecture, les ports, volumes et la taille.
- Le conteneur est non privilégié, sans capability ni device, avec `no-new-privileges` et `seccomp=builtin` explicitement imposés même si le démon est configuré `unconfined`. Un filtre seccomp additionnel est appliqué dans le processus de tests. Le réseau vaut `none`, aucun port n'est publié et le socket Docker n'est jamais monté.
- La racine est en lecture seule. `/input` est le seul bind hôte, strictement en lecture seule, non récursif et limité au workspace aléatoire préparé. `/workspace` (64 Mio) et `/tmp` (16 Mio) sont des `tmpfs` `noexec,nosuid,nodev`.
- Les limites mesurées sont 1 CPU, 512 Mio de mémoire et de swap, 64 PID/threads, `nofile=256:256`, 25 s de compilation, 30 s globales par suite de tests, 5 s de marge de contrôle, 64 Kio de sortie publique et deux conteneurs simultanés par défaut. La plage de configuration est validée et bornée. Lors de la finition MVP, le passage de 0,5 à 1 CPU puis de 15 à 30 s globales de tests a corrigé les timeouts intermittents de démarrage à froid .NET/EF : chaque cas reste dans un sous-processus isolé, une boucle infinie est encore tuée sous 60 s et les limites de mémoire, PID, réseau, montage et sortie restent inchangées.
- La taille disque est imposée par le `tmpfs`. Le test de 80 Mio doit provoquer une erreur d'écriture avant de retourner la valeur attendue. `fsize=64 Kio` a été rejeté après mesure car il empêchait CoreCLR de démarrer ; l'omettre ne rend pas le disque illimité.
- Le runner appelle directement `docker` et `dotnet` avec `ProcessStartInfo.ArgumentList`, sans shell. Roslyn, ses références, la suite et les arguments sont choisis par l'image ou le serveur. La proposition ne fournit jamais de commande, argument, chemin de test ou variable d'environnement.
- En 04D, les suites pédagogiques restent confinées côté serveur et liées à la révision exacte du contenu. Elles sont montées chiffrées par AES-GCM ; la clé aléatoire de 256 bits est transmise uniquement par stdin et effacée. Le parent non dumpable conserve les valeurs attendues ; chaque enfant seccomp reçoit seulement la signature et les arguments du cas courant. Un code soumis ne peut donc pas lire les autres cas ou les réponses attendues depuis son processus.
- L'environnement est construit par liste blanche et ne recopie aucune variable hôte. Le pilote de logs Docker est `none`. Les sorties utilisateur sont capturées tête/queue, expurgées et bornées ; les tests cachés ne quittent le conteneur que sous forme de compteur.
- Toute annulation tue le client puis `rm --force` le conteneur. Le nettoyage vérifie l'absence par `inspect`, puis par une liste filtrée si `inspect` est indisponible. Le workspace est nettoyé indépendamment ; plusieurs erreurs sont agrégées au lieu de se masquer. Au démarrage suivant, la maintenance récupère les conteneurs labellisés et workspaces `run-*` orphelins.
- Le mode `Manual`, actif par défaut et dans Compose, retourne `Unavailable` et exporte uniquement les sources et métadonnées publiques. Il ne crée ni réussite, ni tentative sérieuse, ni score de maîtrise.

### Résultats de la batterie d'abus

La catégorie `CodeRunnerSecurity` couvre 18 cas : programme nominal, compilation invalide, échecs visible/caché, boucle infinie, mémoire, sortie, disque, réseau, fichier hôte, variable secrète, sous-processus, fork, traversal, annulation, politique `inspect`, concurrence, orphelin et contexte Docker absent. Chaque cas vérifie aussi l'absence de conteneur et de workspace. La coupure manuelle de Docker pendant une tentative a révélé puis fait corriger une preuve de nettoyage trop dépendante d'`inspect`.

Le scan d'image reste une porte indépendante : une analyse interrompue ou sans rapport n'est pas équivalente à zéro vulnérabilité. L'audit natif du 28 juillet 2026 a utilisé Trivy `0.70.0`, image scanner `sha256:c0a2b004a57047aff2bc7a8b87d693d368ba40cd10ef9bb1213345f043f416dd`, avec téléchargement de base séparé puis analyse sans réseau. Il a réellement analysé l'image runner `sha256:d34875ea2a6adcd8247bc67ef214fdda7613fda89208629d55bac1e6851bd40c`, Alpine 3.23.5 (37 paquets OS) et 34 manifestes .NET : `0` vulnérabilité critique, code de sortie `0`. Deux timeouts Docker Scout et deux rapports SBOM sans paquets détectés ont été classés non concluants et n'ont pas été utilisés comme preuve. `scripts/scan-code-runner-image.ps1` reproduit la méthode native en empêchant l'accès simultané du scanner au réseau et au socket Docker.

### Risques résiduels

- Docker Desktop, le noyau Linux et le profil seccomp par défaut restent dans la base de confiance ; une vulnérabilité d'évasion inconnue demeure possible.
- L'image SDK est plus grande qu'une image runtime, car Roslyn et les assemblies de référence sont nécessaires. Sa taille augmente le volume à analyser et impose des reconstructions/scans réguliers.
- Les chemins Roslyn/références sont liés aux versions du digest épinglé. Une mise à jour d'image doit les modifier explicitement et rejouer toutes les vérifications.
- Le nettoyage des orphelins suppose une seule instance locale Forge ; deux processus Web concurrents ne sont pas un mode supporté.
- Les contrôles protègent la machine locale, mais ne rendent pas Forge.NET apte à recevoir du code anonyme depuis Internet.

Le contrat public accepte uniquement l'identité/version/révision d'un exercice et jusqu'à huit fichiers `.cs` bornés ; il ne contient ni commande, arguments, chemin, solution ou tests. Depuis 04D, la source serveur produit la suite seulement pour les dix exercices approuvés et refuse toute révision obsolète. `RunExercise` revérifie les combinaisons de statuts, compteurs, noms de fichiers, diagnostics et sorties avant exposition.

Les échecs cachés sont réduits à un compteur expurgé ; aucun nom, code, chemin ou diagnostic caché ne traverse le contrat. Les sorties sont limitées à 64 Kio UTF-8, les diagnostics/échecs visibles à 100, et un retry utilise un GUID et une empreinte de requête. L'historique mémoire ne conserve pas la proposition. Le mode par défaut reste `Manual` et produit `Unavailable`. En mode Docker configuré, seuls les dix exercices 04D peuvent produire une validation automatique ; celle-ci ne vaut jamais maîtrise.

## SqlLab

### Revue renforcée 06A

- L'image SQL Server 2022 CU26 est épinglée par digest. Le service est non-root (`10001:10001`), sans capability, avec `no-new-privileges`, profil seccomp versionné, CPU/mémoire/PID bornés et sans socket ou device.
- SQL Server n'a aucun port hôte et n'appartient qu'au réseau `forge-dotnet-sql-lab-internal` marqué `internal`. Le pont de test distinct n'a aucun secret, publie seulement la boucle locale Windows et est le seul membre de son réseau sortant dédié.
- Aucun volume ou bind de données n'est monté : la couche du conteneur est jetable. Le seul bind SQL est le secret administrateur en lecture seule. SQLite de progression n'est pas visible dans le conteneur.
- Chaque session obtient une base et un login aléatoires. Le login n'est membre d'aucun rôle serveur ou `db_owner`; il reçoit seulement le DML sur `dbo`, avec DDL, exécution, appropriation et définition niés.
- `TRUSTWORTHY` et le chaînage inter-base sont désactivés. Un autre login de session n'est jamais créé comme utilisateur dans la base courante.
- Les requêtes serveur, batches et références en trois parties sont refusés en amont, mais cette garde n'est qu'un contrôle additionnel. Les tests attaquent directement le login SQL pour démontrer les permissions.
- Chaque exécution ouvre une transaction, applique timeout/lock timeout, borne colonnes/lignes/octets, permet l'annulation serveur, observe les effets puis rollbacke. Aucun résultat partiel n'est exposé après quota.
- Le compte `sa` sert uniquement au plan de contrôle Infrastructure. Son secret est généré localement, monté en fichier et absent de Compose rendu, `Config.Env`, navigateur et logs. Web ne reçoit que des vues sans serveur/login/base/mot de passe.
- Les erreurs SQL brutes sont remplacées par des messages bornés et un identifiant de diagnostic. Les logs structurés conservent uniquement catégorie, numéro SQL et diagnostic, jamais la requête.

La catégorie `SqlLabSecurity` démontre SELECT nominal, timeout, résultat massif, annulation, rollback/reset, deux sessions isolées, accès inter-base, DDL serveur, `xp_cmdshell`, scripts externes, tentative de lecture SQLite par bulk, rôles minimaux, absence de secret et validation ordonnée/non ordonnée. Une simple liste noire n'est jamais utilisée comme preuve principale.

Risques résiduels : le plan de contrôle conserve un secret `sa` côté serveur ; la racine SQL doit rester écrivable car SQL Server refuse `tmpfs` pour ses fichiers système ; Docker Desktop, le noyau et SQL Server restent dans la base de confiance. Ces limites interdisent toute exposition Internet et sont détaillées dans `SQLLAB.md`.

### Revue du contenu SQL/EF 06B

- Les douze datasets sont fictifs, déterministes, bornés et ne contiennent ni secret ni chaîne de connexion.
- Le harnais crée une base et un login aléatoires par scénario, n'active ni `TRUSTWORTHY` ni chaînage inter-base et détruit les deux ressources après le test.
- Les scénarios SQL reçoivent seulement les droits DML annoncés par le manifeste. Le scénario EF migrations obtient temporairement les seuls droits DDL nécessaires sur sa base et son schéma ; les tests prouvent l'absence de `sysadmin`, `db_owner`, `ALTER ANY LOGIN`, `ALTER ANY DATABASE` et de prise de possession.
- Les solutions, lignes attendues et variantes négatives restent dans les fichiers réservés au serveur. La projection publique continue d'omettre ces éléments.
- Les requêtes sont bornées par timeout et nombre de lignes. Le dataset de plan vérifie le nom de l'index et l'opérateur `Index Seek`, jamais un coût exact ou une durée fragile.
- Les effets, transactions et resets sont contrôlés sur des bases jetables. Une erreur de nettoyage ne masque pas l'erreur initiale : le harnais les agrège.

Cette revue ne modifie pas la frontière de sécurité de 06A et ne rend pas SqlLab publiable sur Internet. Les preuves et commandes reproductibles sont consignées dans `SQL_EF_CONTENT.md`.

## Intégrité de la maîtrise — 07A

- Le score est une projection serveur issue d’un calcul Domain pur. Web ne reçoit aucun cas d’usage de modification directe et ne peut fournir ni score, ni poids, ni état de porte.
- Les observations runner C# et SqlLab sont append-only. L’identité et le diagnostic sont uniques ; un rejeu identique est idempotent et une divergence est refusée. Les observations DebugLab restent liées à leurs activités persistées.
- Une observation C# conserve seulement version/révision, compteurs, statut, diagnostic et empreinte SHA-256. Une observation SQL conserve seulement les métadonnées bornées et l’empreinte SHA-256. Ni code source, ni requête, ni sortie brute n’entre dans la projection.
- Une déclaration manuelle, un runner indisponible, un examen autoproclamé ou un livrable sans vérification serveur ne produit aucune preuve admissible. Le mode manuel ne crée toujours aucun score.
- Le niveau d’indice est reconstruit à l’instant de la tentative. Une solution consultée reste traçable, met la pratique du même exercice à zéro et retire cet exercice du compte sans aide ; une nouvelle projection ne peut pas effacer cette contamination.
- La variété exige trois exercices autonomes distincts et la récence une pratique automatique sans aide dans les 30 jours. Les répétitions ont un rendement décroissant, les observations expirent du score après 90 jours et un quiz récent ne renouvelle pas une pratique ancienne.
- Les composantes absentes valent zéro, les compétences critiques ne se compensent pas et chaque porte énumère ses conditions manquantes. Depuis 07C, seules les preuves d’examen produites par le moteur serveur sont admises ; les livrables sans producteur restent fermés.
- Chaque projection fige l’identifiant, la version et la révision de politique avec son JSON. Les anciennes projections sont conservées ; un changement de jour, de preuves ou de politique produit une nouvelle clé idempotente au lieu de réinterpréter un score passé.

La catégorie `MasteryAntiGaming` couvre quiz faciles, indices H1–H4, solution, déclarations aléatoires, boucle sur un item, récence, compétence critique, composante absente, faux examen, livrables manquants, bornes, arrondis, rejeu et version. Le test d’intégration vérifie en plus l’absence de code/requête dans SQLite, l’immuabilité des snapshots, la concurrence et le redémarrage. Le détail reproductible figure dans `MASTERY.md`.

## Révisions — 07B

- Une carte fige l’identité, la version et la révision de sa source. La génération déterministe est idempotente ; une source supprimée ne rend pas son snapshot illisible et une nouvelle révision ne remplace pas l’ancienne.
- La projection publique omet la réponse attendue. Elle n’est renvoyée qu’après une soumission côté serveur, jamais dans la file initiale ou les choix publics.
- L’historique append-only conserve une empreinte SHA-256 de la réponse, pas la réponse brute. Les entrées et snapshots sont bornés et les caractères de contrôle inattendus sont refusés.
- Chaque réponse vérifie profil, carte et version attendue dans une section d’écriture sérialisée. Deux réponses concurrentes produisent une tentative et une erreur explicite, jamais deux gains.
- Une autoévaluation, un rappel d’erreur ou une carte personnelle ne produit aucun score. Seule une question diagnostique ratée à choix, vérifiée côté serveur, peut devenir une preuve `ReviewEngine` de rétention espacée.
- Les cartes personnelles et leurs réponses privées restent dans SQLite local. Aucun service externe, tracking, télémétrie, série quotidienne ou mécanisme culpabilisant n’est ajouté.
- Les examens et mesures de dashboard sont ajoutés en 07C sans changer la règle : une autoévaluation reste sans effet sur la maîtrise. Le détail des sources, limites et tests de 07B figure dans `REVIEWS.md`.

## Intégrité des examens et métriques — 07C

- La seed aléatoire de 256 bits est créée côté serveur. Seul son engagement SHA-256 est exposé pendant la tentative ; seed, algorithme et engagement sont réunis dans le rapport final pour permettre un audit du tirage figé.
- L’échéance UTC persistée et le `TimeProvider` serveur gouvernent chaque transition. Une horloge client, une actualisation ou un redémarrage ne prolonge pas la durée ; un résultat runner reçu hors délai est refusé.
- Une seule tentative active est autorisée par profil. Les mutations vérifient profil, GUID opaque et version attendue ; la finalisation écrit statut et rapport dans une même transaction et résiste à la double fin concurrente.
- Pendant une tentative active, `IExamAccessPolicy` refuse les cas d’usage Practice, y compris par URL directe et pour chaque mutation d’indice ou de solution. La projection d’examen ne contient jamais ces aides.
- Les sources privées, solutions et suites de tests restent serveur. Une soumission active renvoie seulement un accusé ; le rapport différé expose des compteurs bornés, jamais nom, code, valeur attendue ou diagnostic caché.
- 07C n’ajoute aucun log contenant code soumis, réponse, seed active, solution ou cas de test. Les mutations Blazor restent couvertes par l’antiforgery ASP.NET et l’encodage Razor.
- Une aide déclarée interdit réussite et preuve de maîtrise. Aucune caméra, capture d’écran, surveillance intrusive ou promesse de blocage infaillible du presse-papiers n’est ajoutée.
- Le dashboard relit uniquement les preuves persistées. Les taux et moyennes sans échantillon restent indisponibles ; le temps actif ignore les intervalles inter-contextes, négatifs ou supérieurs au seuil d’inactivité. Une porte critique reste une conjonction de conditions, jamais une moyenne compensable.

La catégorie `ExamIntegrity` attaque seed/tirage, échéance et reprise, fuite de rapport/tests cachés, accès direct aux aides, double fin, abandon, timeout, redémarrage, échec vers révision, métriques vides/inactives et compensation critique. Les limites et commandes reproductibles figurent dans `EXAMS_DASHBOARD.md`.

### Extension de l’examen 4 SQL/EF — 08

- Le snapshot d’un item fige `ExamSubmissionKind`. Une valeur inconnue, un nom de fichier incompatible ou une révision obsolète est refusé avant exécution. SQL ne peut pas être redirigé vers le runner C# et EF Core ne peut pas être exécuté dans le processus Web.
- `SqlLabExamRunner` recharge l’attente privée par identité/version/révision, utilise exclusivement une session SqlLab jetable et détruit cette session avec un jeton non annulable. Un échec de nettoyage refuse le résultat ; deux erreurs sont agrégées sans masquer l’échec initial.
- Le blueprint et les vues actives omettent lignes attendues, requêtes solutions et tests. Les logs SqlLab restent sans corps de requête ; le rapport ne reçoit que statut et compteurs bornés.
- Les deux items EF Core sont compilés et exécutés par le runner 04C sans réseau, avec racine en lecture seule, tmpfs bornés, utilisateur non-root, seccomp et nettoyage inchangés. Ils utilisent SQLite en mémoire à l’intérieur du conteneur et n’obtiennent aucune connexion vers SQL Server ou SQLite de progression.
- L’image ajoute uniquement `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10 et `SQLitePCLRaw.bundle_e_sqlite3` 3.0.4. Le compilateur accepte les assemblies EF/SQLite publiées selon une liste blanche de préfixes ; le nom de suite, les cas et les arguments restent choisis côté serveur.

Le 30 juillet 2026, l’image `sha256:64289bc26b73a208f5a5f3029c5fdf5698a24d89dee20a402b31a963df48dc7f` a repassé les 18 tests `CodeRunnerSecurity`. Trivy 0.70.0, avec base téléchargée séparément puis analyse hors réseau, a détecté `0` vulnérabilité critique sur Alpine 3.23.5, 37 paquets OS et 34 manifestes .NET. Les six starters/solutions SQL ont été exécutés sur des sessions jetables et les deux paires EF dans le runner isolé.

## Revue sécurité du contenu S11–S20 — 09

- Les exemples API séparent DTO, contrôleur et règle ; la validation produit Problem Details sans stack trace. Les tests couvrent 400, 401, 403, 404 et 201, afin qu’un refus d’authentification ne puisse pas être confondu avec un refus de rôle.
- Les preuves d’authentification versionnées sont exclusivement factices et nommées comme telles. En conteneur, l’application lit des fichiers montés hors Git, bornés à 4 Kio ; elle compare les octets à temps constant et ne journalise jamais la valeur. Le rôle est fixé par la preuve reconnue et ne vient pas d’un en-tête libre.
- Les exercices de sécurité enseignent liste blanche de tri, redirection locale, message de connexion uniforme, propriétaire ou administrateur et redaction complète. Aucun exemple vulnérable n’est présenté comme correct et aucune procédure offensive n’est requise.
- Le Dockerfile du laboratoire utilise deux images .NET 10 épinglées par digest, un build multi-stage et `USER $APP_UID`. Compose publie seulement sur `127.0.0.1`, monte les secrets en lecture seule, active racine en lecture seule, tmpfs, capacités supprimées, `no-new-privileges`, limites CPU/mémoire/PID, `restart: "no"` et health check.
- La pipeline limite le jeton à `contents: read`, désactive la persistance des credentials de checkout, ne consomme aucun secret, s’arrête sur chaque code non nul, borne la rétention de l’artefact et conditionne la répétition de livraison aux preuves vertes et à un environnement protégé.
- Les solutions, cas cachés et réponses d’entretien restent privés selon les contrats existants. Les deux nouvelles banques d’examen n’ajoutent aucune clé ou solution à la projection publique.
- Les laboratoires restent locaux, sans Azure ni service distant obligatoire. Le bac à sable Git opère dans un dossier temporaire distinct et le supprime ; aucun conflit n’est provoqué dans le dépôt de travail.

La validation effective a contrôlé l’utilisateur non-root, la racine en lecture seule, 64 PID, 256 Mio, les capacités supprimées et la santé HTTP avant suppression du conteneur, du réseau et des clés factices temporaires. Le détail reproductible et les limites figurent dans `CONTENT_S11_S20.md`.

## Revue sécurité du contenu S21–S24 — 10

- Azure demeure un support facultatif. Forge.NET ne possède aucun abonnement, identifiant, rôle ou ressource cloud et ne lance aucune création. Le starter et lʼincident de référence fonctionnent hors ligne.
- Le plan Bicep ne contient aucune valeur dʼidentité ou donnée personnelle. Les paramètres nécessaires à une répétition réelle restent sans valeur commitée ; les services applicatifs utilisent une identité système et des rôles à définir au périmètre minimal.
- Storage refuse lʼaccès Blob public et les clés partagées. Azure SQL refuse le réseau public, exige TLS 1.2 et prévoit une administration Entra. Key Vault utilise RBAC et la protection contre la purge. Aucun exemple ne recopie une valeur sensible dans la configuration ou les logs.
- Une répétition réelle exige un groupe de ressources dédié, un avertissement de facturation, un budget/une alerte, un propriétaire, une heure limite, une suppression explicite et une vérification finale. Une alerte de budget nʼest jamais assimilée à un arrêt des dépenses.
- Les logs pédagogiques utilisent uniquement des identifiants `forge-fake-*`, des opérations normalisées, statuts et durées. Aucun corps, en-tête dʼauthentification, paramètre SQL, donnée de CV ou identité réelle nʼest nécessaire.
- Le kit carrière minimise les données, avertit sur le caractère personnel dʼun export et refuse les formes courantes de courriel ou téléphone. Son exemple est fictif ; lʼutilisateur doit encore inspecter contenu, métadonnées et historique avant publication.
- Questions dʼentretien, réponses modèles, solutions, tests cachés et banques dʼexamen conservent les frontières privées existantes. La défense orale et le déploiement réel restent manuels et ne créent aucune preuve automatique.
- Le projet final nʼest pas fourni. La grille exige autorisation positive/négative, configuration sûre, logs sobres et revue contradictoire, sans proposer dʼarchitecture distribuée avancée ou de parcours post-embauche implémenté.

Les tests `ContentS21S24` contrôlent volumes, niveaux, références, garde-fous IaC, signatures caractéristiques de credentials, coûts, confidentialité carrière et absence de remise finale. `Verify-LocalMode.ps1` construit le starter, inspecte les ressources attendues et résout lʼincident sans réseau ni ressource Azure. La matrice et les limites reproductibles figurent dans `CONTENT_S21_S24.md`.

## Application et contenu

- Blazor applique encodage de sortie et politique CSP ; Markdown passe par un rendu avec HTML brut désactivé ou assaini.
- Protection CSRF pour mutations, cookies locaux sécurisés si une session est utilisée, limites de taille et validation serveur.
- Chemins de contenu canonicalisés et confinés ; liens symboliques et archives avec traversal refusés.
- Tests cachés/solutions exclus des réponses et bundles publics avant déverrouillage.
- Dépendances verrouillées, auditables et mises à jour par lots testés.

Le validateur de contenu v1 canonicalise la racine et les références, refuse chemins absolus, URI, traversal, liens symboliques/points de réanalyse et HTML brut, puis impose UTF-8, profondeur, taille de fichier et volume de lot bornés. Ses diagnostics ne recopient jamais une valeur de contenu, une solution ou un test caché.

Le catalogue n'est publié qu'après deux validations du lot et la résolution de ses graphes. Sa projection publique omet réponses modèles, indices, solutions et chemins cachés ; son index de recherche utilise seulement les métadonnées publiques. Un rechargement invalide conserve le snapshot précédent et les journaux CLI restent limités aux IDs, types, compteurs, révision, durée et diagnostics.

Le lecteur refuse un identifiant absent du catalogue publié, confine le chemin canonique sous la racine de contenu, refuse les points de réanalyse, impose UTF-8 strict et borne une leçon à 256 Kio. Son parseur transforme le Markdown en blocs typés : aucun HTML brut n'est rendu, un lien non HTTPS/local/fragment devient du texte et Razor encode chaque valeur. La réponse du quiz reste côté serveur jusqu'à la soumission ; solutions et tests cachés ne sont jamais lus par cette source.

Les mutations de note, signet et activité passent par le circuit Blazor avec antiforgery ASP.NET et validation serveur des tailles et identifiants. L'hôte émet une CSP restrictive (`default-src 'self'`, objets interdits, ancêtres de cadre interdits), `nosniff` et une politique de référent sans fuite. Une erreur d'autosauvegarde est affichée et n'est jamais masquée comme un succès.

La banque de diagnostic sépare physiquement questions publiques, clé attendue et paramètres du barème. Le chargeur privé contrôle leur correspondance au démarrage. Le plan figé en SQLite ne contient jamais la clé. En 03B, la source privée remet la clé uniquement au cas d'usage serveur ; une session active est refusée avant ce chargement. La projection Web contient seulement scores, intervalles et compteurs agrégés par domaine : aucun identifiant de question, choix utilisateur, réponse attendue ou texte corrigé.

`DiagnosticEvaluations` persiste le snapshot des poids/seuils et le rapport agrégé, jamais la clé privée. La révision du barème couvre néanmoins la clé et les paramètres, ce qui détecte toute modification. Un rapport existant est relu tel quel ; une session ancienne sans rapport et sans révision compatible échoue fermée plutôt que d'être recalculée avec un autre barème. Les logs restent exempts de réponse, clé, corps de question et détail de correction.

Les libellés d'évaluation sont volontairement prudents : confiance au plus modérée, mode réduit provisoire, diagnostic incomplet signalé et faiblesse critique non compensable. Le rapport ne contient aucune promesse d'emploi ou de salaire ni aucune maîtrise globale.

En 03C, le curriculum de planification est confiné sous `content/`, borné, lu en UTF-8 strict et refusé si ses propriétés, domaines ou prérequis sont invalides. `WeeklyPlans` persiste uniquement le snapshot agrégé : aucun pseudonyme, objectif professionnel, choix diagnostique ou clé attendue. Les ajustements de charge et la version attendue sont validés côté serveur. Une lacune critique reste planifiée, une version périmée échoue fermée et un plan accepté devient immuable. Les avertissements restent factuels, sans manipulation, promesse d'emploi ni donnée externe.

En 04A, la source de pratique privée repart uniquement d'un exercice publié, confine tous ses fichiers sous la racine de contenu, refuse traversal et points de réanalyse, impose UTF-8 strict et une taille maximale. Elle ne lit jamais les tests cachés. Les indices non consultés, la solution, l'explication modèle, la variante et leurs chemins restent absents des vues publiques jusqu'à la transition autorisée.

Chaque mutation recharge l'activité du profil local, vérifie l'identité exacte de l'exercice, sa révision et la version attendue. Le gel de réflexion, l'ordre H1–H4, les deux tentatives sérieuses distinctes et le délai fondé sur le `TimeProvider` serveur sont revérifiés dans Domain. Les historiques SQLite sont append-only et une concurrence produit une erreur explicite, pas un doublon. Les entrées sont bornées ; les identifiants internes sont aléatoires et non exposés. Proposition, observations, explication personnelle, variante et solution ne sont jamais journalisées. Les événements Blazor utilisent l'antiforgery ASP.NET existant. Aucun code utilisateur n'est compilé, testé ou exécuté dans 04A.

La minuterie repose sur une échéance UTC créée avec le `TimeProvider` serveur et persistée. Avant chaque réponse ou transition, le serveur recharge et expire la section si nécessaire ; une valeur d'horloge client n'est jamais acceptée. L'accès vérifie le profil local, la session, la question figée et l'option publique. Les journaux n'incluent ni réponse utilisateur, ni clé attendue, ni corps de question, et aucune surveillance intrusive n'est mise en place.

## Données et confidentialité

- Aucune télémétrie externe par défaut.
- Données locales minimales ; pas de mot de passe nécessaire pour le profil mono-utilisateur.
- Logs structurés avec redaction ; aucune réponse d'examen, code soumis, secret ou donnée CV complète.
- Sauvegardes avec manifeste, version et checksum. Une archive restaurée est validée dans une zone temporaire avant remplacement.
- L'export de carrière avertit qu'il peut contenir des données personnelles.

## Examens et intégrité

La désactivation du copier-coller et le plein écran sont dissuasifs, pas inviolables. La crédibilité repose sur le tirage auditable, les tests cachés, l’échéance serveur, la variété, la défense orale et les réévaluations. Toute aide déclarée est conservée dans le rapport et invalide la preuve de maîtrise.

## Secrets et configuration

- Aucun secret commité ; fichiers de développement ignorés et exemples factices.
- Secrets locaux via variables/processus adapté ou user-secrets, jamais transmis aux runners.
- Ports, origines et chemins restrictifs par défaut.
- Les options dangereuses échouent fermées avec message explicite.

## Réponse aux incidents locaux

1. Arrêter les runners et isoler le conteneur concerné.
2. Préserver uniquement métadonnées et logs non sensibles nécessaires.
3. Révoquer les secrets potentiellement exposés.
4. Corriger, ajouter un test de non-régression et reconstruire l'image.
5. Documenter cause, impact, correction et prévention dans le runbook.

## Revue requise avant le MVP

- Vérification manuelle des options Docker effectives (`inspect`).
- Analyse de dépendances et d'image sans vulnérabilité critique non acceptée.
- Tests d'abus CodeRunner/SqlLab verts sur Windows cible.
- Vérification que solutions/tests cachés n'apparaissent pas dans réseau, logs ou artefacts client.
- Exercice de sauvegarde/restauration et simulation de corruption.

## Revue finale de sécurité — incrément 11

La qualification du 6 août 2026 a rejoué les frontières CodeRunner, SqlLab, sauvegarde et exposition Web sur le poste Windows cible.

### Dépendances et images

- `dotnet list package --vulnerable --include-transitive` ne remonte aucun paquet vulnérable dans les huit projets.
- L'audit des paquets dépréciés ne remonte rien pour les projets applicatifs et le runtime. `xunit` 2.9.3 est classé legacy par NuGet sans vulnérabilité connue ; une migration vers xUnit v3 doit être isolée et suivie d'une requalification complète du harnais.
- Trivy 0.70.0, avec base CVE fraîche puis analyse hors réseau, trouve zéro vulnérabilité critique dans l'image Web finale `sha256:fd7c47b1ad6090aa72a4ae9b7c020f4de9c39511f1d4f2042e4e0688b47b3d1c` : Alpine 3.23.5, 21 paquets OS et trois manifestes .NET inspectés.
- Le même protocole trouve zéro vulnérabilité dans l'image CodeRunner finale `sha256:43e075c820a78f5cb0f61e3c6923b9c5bd3833f3a20bd9e168a0028665ed181a` : Alpine 3.23.5, 37 paquets OS et 34 manifestes .NET. Le scanner Trivy utilisé est épinglé par `sha256:be1190afcb28352bfddc4ddeb71470835d16462af68d310f9f4bca710961a41e`.
- SQL Server est passé de CU21 à CU26, digest `sha256:ba4c8329f48fb8f02e1416be6a930ebfd71268caee78aa985f3af4315e457c89`. Le scan de l'image de base CU26 trouve zéro vulnérabilité critique sur Ubuntu 22.04, 184 paquets OS et trois binaires Go.

Le scan direct de l'image SqlLab dérivée a été interrompu par un `SIGBUS` du moteur Docker et n'est donc pas déclaré réussi. La preuve compensatoire montre que l'inventaire des paquets de la base et de l'image finale a le même SHA-256 (`35b78b7ade817c0b0ee6f024a12f195814b14b235c49bc508395474877b0982b`), que les trois binaires Go ont des empreintes identiques et que l'historique dérivé ajoute uniquement retrait de capability, entrypoint, pont TCP, utilisateur et configuration. Il s'agit d'une inférence forte, pas d'un remplacement permanent du scan direct : celui-ci doit être rejoué lorsque le stockage Docker est stabilisé.

### Contrôles effectifs et données

- L'image Web s'exécute sous l'utilisateur `1654`, racine en lecture seule, capacités supprimées, `no-new-privileges`, volume de données dédié et port publié uniquement sur `127.0.0.1`.
- Le conteneur SqlLab inspecté utilise `10001:10001`, réseau interne, seccomp, capacités supprimées, `no-new-privileges`, 2 Gio de mémoire, 2 CPU et 512 PID. Les tests dédiés couvrent timeout, quota, annulation, redaction, rollback, reset et destruction de session.
- Les six tests de restauration refusent archive invalide, traversal, checksum faux, corruption même avec checksum recalculé et version de manifeste non supportée, en plus de l'aller-retour nominal.
- Les recherches de secrets n'ont détecté aucun secret littéral commité. Les marqueurs `NotImplementedException` restants sont confinés aux starters pédagogiques ; les tests prouvent que ces starters compilent sans réussir et que les solutions approuvées passent dans le runner isolé.
- L'installation vierge n'a émis aucun log final de niveau `Error` ou `Critical` et aucun secret SQL, jeton Bearer ou code soumis. Les tests Web contrôlent CSP, `nosniff`, politique de référent, absence de tests cachés et absence de solution avant transition autorisée.

Aucune de ces mesures ne rend Forge.NET publiable sur Internet. Les clés Data Protection persistées dans le volume Linux restent dépendantes des droits et du chiffrement disque du poste ; l'avertissement ASP.NET correspondant est conservé au lieu d'être masqué par un secret dans Compose.
