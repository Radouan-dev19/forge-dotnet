# Fiches techniques, entretien blanc et checklist du jour J

Ce dossier accompagne le [programme de 5 h 10](/prep/icube-plan-veille-001).
Pendant chaque créneau, lis seulement la fiche correspondante. Réponds d'abord à voix haute,
puis compare avec les repères. Une bonne restitution comporte une idée, un exemple et une
vérification ; réciter des termes seuls ne permet pas de mesurer ta compréhension.

Les questions sont des entraînements déduits de l'annonce, pas des questions obtenues auprès du
recruteur. Les réponses orales et la checklist restent une auto-évaluation sans preuve de maîtrise.
Les exercices liés conservent leur mécanisme de tentative, indices, solution et révision à blanc.

## C# et async — expliquer le code que tu écris

**À quoi sert une interface ?** Elle décrit un contrat. Le service consommateur dépend de ce
contrat et reçoit une implémentation, ce qui facilite le remplacement d'un adaptateur ou d'une
dépendance en test. Exemple : un service métier reçoit une interface d'horloge pour tester une
échéance. Ajouter une interface à chaque classe sans besoin ne rend pas automatiquement la conception
meilleure. La composition assemble des comportements ; l'héritage doit exprimer une relation
de substitution cohérente.

**Liste, dictionnaire et LINQ ?** Une liste représente une séquence ; un dictionnaire associe une
clé à une valeur et permet une recherche généralement rapide. LINQ exprime filtres, projections,
tris et agrégations. Sur une séquence en mémoire, `Where` et `Select` sont souvent évalués
lors de l'énumération : une variable contenant la requête n'est pas toujours un instantané.
Un tri coûte généralement O(n log n), un simple parcours O(n). Vérifie liste vide, doublons,
ordre et absence de modification de l'entrée si le contrat le demande.

**Que font Task et await ?** Une `Task` représente une opération pouvant être inachevée.
Si une entrée-sortie n'est pas terminée, `await` suspend la méthode sans immobiliser le thread
pendant cette attente. Il ne crée pas automatiquement un thread. `.Result` et `.Wait()`
bloquent le thread ; dans une API, cela peut épuiser les threads disponibles. Ne présente pas
le deadlock comme inévitable dans ASP.NET Core. `CancellationToken` transmet une demande
d'annulation que les opérations doivent observer ; il ne tue pas arbitrairement un thread.

**Comment traiter null et les exceptions ?** Définis le contrat : absence normale, entrée invalide
ou incident. Les annotations nullable aident à la compilation mais ne valident pas une entrée HTTP.
N'avale pas une exception en retournant silencieusement un succès. Pour relancer l'exception
courante en conservant sa pile, utilise `throw;`.

Relance : « Deux appels asynchrones sont-ils indépendants ? » Si oui, `Task.WhenAll` peut les
attendre ensemble ; si le second dépend du premier, il faut respecter cet ordre.
Ne lance pas deux opérations simultanées sur le même `DbContext`.

Support ciblé : [Async](/learn/async-fundamentals-001).
Pratique supplémentaire : [LINQ et les trois plus grands](/practice/csharp-linq-top-three-001).

## ASP.NET Core — du HTTP à la règle métier

**Décris une requête.** Le pipeline de middleware traite la requête ; routage, authentification,
autorisation et endpoint interviennent selon la configuration. L'endpoint reçoit un DTO, valide
l'entrée et appelle le cas d'usage. Le service métier applique la règle, l'adaptateur accède aux
données, puis l'API renvoie un contrat et un statut. Un DTO évite d'exposer sans contrôle toute
l'entité de persistance, notamment les champs que le client ne doit pas modifier.

**Transient, Scoped ou Singleton ?** Transient : nouvelle instance à chaque résolution.
Scoped : une instance par portée, généralement la requête HTTP dans une API.
Singleton : une instance partagée pendant la vie du conteneur, donc un état mutable partagé
doit être sûr en concurrence. `AddDbContext` enregistre normalement un contexte Scoped.
Injecter directement ce contexte dans un singleton crée un conflit de durée de vie ; un
traitement d'arrière-plan doit créer une portée adaptée ou utiliser une factory.

**Quels statuts justifier ?** `200` pour une réponse réussie avec contenu, `201` pour une
création avec une adresse de ressource quand elle existe, `204` pour un succès sans corps,
`400` pour une entrée invalide, `404` pour une ressource absente et `409` pour un conflit
avec l'état courant. Dans un contrôleur avec `[ApiController]`, certaines erreurs de validation
du modèle produisent automatiquement un `400` ; vérifie la configuration du type d'endpoint.

**Que signifie idempotent en HTTP ?** Répéter une même opération a le même effet final attendu.
Un PUT remplace l'état d'une ressource ; un POST de création n'est pas idempotent par défaut.
Des DELETE répétés peuvent donner des statuts différents tout en laissant la ressource supprimée.

Relance : « Le client soumet un champ de rôle ou de propriétaire qu'il ne devrait pas contrôler :
que fais-tu ? » Tu limites le DTO et fixes les valeurs sensibles côté serveur après autorisation.
Un message d'erreur public ne doit révéler ni secret ni trace interne.

Supports : [DTO](/learn/api-controllers-dtos-001),
[DI](/learn/api-di-lifetimes-001), [validation et erreurs](/learn/api-validation-problem-details-001).

## SQL et EF Core — raisonner sur les données réellement lues

**INNER JOIN ou LEFT JOIN ?** La première ne conserve que les correspondances ; la seconde
conserve toutes les lignes de gauche et met NULL à droite sans correspondance. Attention :
filtrer ensuite dans WHERE une colonne de droite en exigeant une valeur peut supprimer ces lignes.
`GROUP BY` regroupe avant agrégation ; `HAVING` filtre les groupes.
Une clé étrangère protège une relation ; un index peut accélérer certaines lectures mais coûte
du stockage et du travail lors des écritures.

**DbContext, tracking et migrations ?** Le contexte représente une unité de travail courte,
suit les entités chargées en mode tracking et applique les changements avec `SaveChangesAsync`.
Il n'est pas thread-safe. Une migration décrit une évolution du schéma ; elle se relit et se teste
avant application. Pour une lecture d'entités sans modification, `AsNoTracking` évite le suivi ;
une projection permet de demander seulement les données nécessaires.

**IQueryable contre IEnumerable ?** Avec EF, un `IQueryable` décrit une expression que le
provider tente de traduire en SQL. `IEnumerable` fournit un contrat d'énumération ; il ne signifie
pas, à lui seul, que toutes les données sont déjà chargées. `ToListAsync` matérialise le résultat.
Filtrer après cette matérialisation peut charger bien plus de lignes que nécessaire.

**Comment corriger un N+1 ?** Observe les requêtes émises : une lecture initiale suivie d'une
requête par élément est un indice. Choisis projection, chargement adapté ou regroupement selon
le besoin, puis mesure à nouveau. `Include` n'est pas un remède universel : plusieurs collections
peuvent produire beaucoup de lignes.

**Transaction et concurrence ?** Une transaction rend un ensemble d'écritures atomique.
Deux utilisateurs peuvent néanmoins lire le même état avant leurs modifications.
Un jeton de concurrence permet de détecter une écriture devenue obsolète et de traiter le conflit
plutôt que d'écraser silencieusement l'autre modification. Pour paginer, utilise un ordre stable
avec une clé unique de départage.

Relance : « Comment prouves-tu que ta requête est meilleure ? » Compare résultat métier, SQL émis,
nombre d'appels, volume ramené et temps sur un jeu représentatif.
Support : [SQL/EF](/learn/ef-core-data-access-001).

## Tests et revue — prouver un comportement

**Qu'est-ce qu'un bon test unitaire ?** Il vérifie un comportement précis avec un résultat attendu
clair, s'exécute de façon indépendante et reste répétable. Arrange prépare, Act appelle, Assert
vérifie. `[Fact]` décrit un cas ; `[Theory]` permet de paramétrer plusieurs exemples.
Pour une borne inclusive, teste la borne, la valeur juste en dessous et celle juste au-dessus.

**Unitaire ou intégration ?** Le premier isole une règle ; le second vérifie l'assemblage de
composants, par exemple route HTTP, sérialisation, validation et base de test.
`WebApplicationFactory` peut héberger l'application pour des tests HTTP.
Un faux dépôt ne prouve ni traduction SQL ni contraintes du moteur réel.
Le provider EF InMemory ne reproduit pas toutes les propriétés d'une base relationnelle.

**Quel double choisir ?** Un stub fournit une réponse contrôlée, un mock permet de vérifier
une interaction attendue, un fake propose une implémentation simplifiée. La terminologie varie
selon les outils ; explique surtout quel comportement ton double remplace et ce que ton test prouve.

**Comment faire une revue utile ?** Priorise correction, autorisation, données, concurrence,
performance et tests avant le style. Donne un scénario reproductible : « Avec une quantité zéro,
la commande est acceptée ; voici le test qui devrait échouer. » Ajoute le test de non-régression
avant ou avec le correctif. Une couverture élevée ne garantit pas la pertinence des assertions.

Relance : « Un test passe seul mais échoue avec les autres : pourquoi ? » Cherche état partagé,
base non réinitialisée, dépendance à l'ordre ou à l'horloge.
Supports : [xUnit](/learn/tests-xunit-aaa-001), [intégration](/learn/tests-integration-database-001).

## Vue 3 et TypeScript — suivre les données dans un composant

**Comment lire un composant ?** `<script setup lang="ts">` contient la logique typée,
le template décrit le rendu et le style règle la présentation. `ref` accepte une valeur
primitive ou un objet ; son accès dans le script utilise `.value`.
`reactive` produit un proxy réactif d'objet. Une déstructuration ordinaire d'une propriété
primitive d'un objet reactive perd le lien ; explique quand `toRefs` est utile.

**computed ou watch ?** `computed` exprime une valeur dérivée mise en cache selon ses dépendances
réactives. `watch` déclenche un effet en réaction à un changement, par exemple une recherche HTTP.
Les props descendent du parent ; les événements remontent une intention. L'enfant ne doit pas
prendre silencieusement possession de l'état du parent en le modifiant.

**Comment consommer une API ?** Définis chargement, succès, vide et erreur ; avec fetch, vérifie
`response.ok`, car un statut HTTP d'erreur ne rejette pas forcément la promesse.
Traite aussi le cas de deux recherches dont les réponses arrivent dans le désordre : annulation
ou identification de la réponse encore pertinente. Les types TypeScript sont retirés à
l'exécution ; ils ne valident pas automatiquement un JSON réseau.

**État local ou partagé ?** Un champ de filtre peut rester local. Un état utilisé dans plusieurs
écrans peut être partagé via un composable ou un store tel que Pinia selon sa portée.
Le routeur choisit l'écran ; une garde de navigation améliore le parcours mais ne remplace pas
l'autorisation serveur. Dans `v-for`, utilise une clé stable liée à l'élément.

Relance : « Pourquoi computed pour une liste filtrée ? » Parce que la liste est dérivée des
commandes et du filtre, sans recopier un deuxième état à maintenir.
Support : [Vue/TypeScript](/learn/week-0/week0-vue-typescript-001).

## Service Bus et microservices — anticiper pannes et doublons

**Queue ou topic ?** Une queue distribue le travail entre consommateurs concurrents.
Un topic permet plusieurs abonnements indépendants ; chacun reçoit les messages correspondant à
ses filtres. Un abonnement peut lui-même avoir plusieurs consommateurs concurrents.
Cela ne signifie jamais qu'un message sera traité exactement une fois.

**PeekLock et acquittement ?** Le message est reçu avec un verrou temporaire.
Après réussite, le consommateur le complète. Une expiration de verrou, un abandon ou un échec
d'acquittement peut permettre une nouvelle livraison. Les traitements trop longs peuvent nécessiter
un renouvellement du verrou. Les messages irrécupérables ou ayant dépassé la limite de livraison
peuvent aller dans la dead-letter queue pour diagnostic et éventuel rejeu.

**Comment éviter deux effets métier ?** Associe une identité stable à l'opération et enregistre
durablement le traitement, avec une contrainte d'unicité et une transaction adaptée si la
modification métier est dans la même base. Une simple vérification « déjà vu ? » suivie d'une
écriture sans protection contre la concurrence peut laisser passer deux consommateurs.
La déduplication des envois du broker ne couvre pas tous les scénarios de nouvelle livraison.

**Pourquoi une outbox ?** Écrire la commande et l'événement à publier dans une même transaction
locale évite la fenêtre « base validée, publication oubliée ». Un processus publie ensuite
l'outbox, avec reprise sur erreur. Il peut publier deux fois : le consommateur reste idempotent.

**Pourquoi des microservices ?** Des frontières métier et des déploiements indépendants peuvent
faciliter l'évolution de domaines distincts. En contrepartie : pannes réseau, cohérence éventuelle,
observabilité distribuée et contrats à faire évoluer. Évite la découpe en service contrôleurs,
service règles et service base : elle crée souvent une chaîne de dépendances techniques.
Un monolithe modulaire peut être adapté lorsque l'indépendance de déploiement n'apporte pas assez.

Relance : « Le service de notification reste indisponible une heure : que surveilles-tu ? »
Âge et taille de la file, erreurs, tentatives, lettres mortes et capacité de rattrapage.
Les réessais doivent être bornés, espacés et réservés aux erreurs transitoires.
Supports : [Service Bus](/learn/week-0/week0-azure-service-bus-001),
[frontières](/learn-senior/senior-boundaries-001).

## Azure DevOps, Azure et IIS — livrer et diagnostiquer

**Décris ton pipeline.** À partir d'une PR ou d'un commit, restaurer les dépendances, compiler,
exécuter les tests, publier un artefact versionné, puis déployer cet artefact dans un environnement
contrôlé. Une condition de déploiement doit empêcher la promotion après un échec de tests.
Les stages regroupent des jobs, eux-mêmes composés de steps. Repos héberge le code, Boards suit
le travail, Pipelines automatise et Artifacts héberge notamment des packages.

**Où sont les secrets ?** Ils n'appartiennent ni au dépôt ni aux logs. Utilise les mécanismes
de secrets de la plateforme et des permissions limitées. Key Vault stocke des secrets, clés
et certificats. Une identité managée permet à une application hébergée sur Azure d'obtenir
une identité sans gérer elle-même un mot de passe ; il faut toujours lui attribuer les droits utiles.

**Quel hébergement ?** App Service héberge notamment des applications web avec une exploitation
managée. Azure SQL fournit une base relationnelle, Storage des services comme les blobs,
Service Bus la messagerie. Le choix dépend du besoin, du mode de livraison, des contraintes réseau
et d'exploitation. Connais les logs, métriques et traces corrélées pour suivre une demande.

**Quel rôle pour IIS ?** Un site possède des liaisons adresse/port/nom d'hôte et éventuellement
un certificat. Le pool porte le processus et son identité. Le Hosting Bundle installe notamment
le module ASP.NET Core nécessaire. En mode in-process, l'application s'exécute dans le processus
IIS ; en out-of-process, IIS relaie vers Kestrel. IIS n'est donc pas toujours seulement un proxy
vers un processus Kestrel distinct.

**L'application ne démarre plus : que vérifier ?** Statut et erreur exacts, événements et logs,
runtime/module installés, configuration d'environnement, accès aux fichiers et à la base.
Les logs de démarrage détaillés se manipulent temporairement et sans secrets.
Pour un retour arrière, vérifie aussi la compatibilité du schéma de données : redéployer l'ancien
binaire ne suffit pas si une migration l'a rendu incompatible.

Relance : « Quel artefact déploies-tu en production ? » Une version identifiable déjà testée,
avec la configuration appropriée à l'environnement.
Supports : [DevOps](/learn/week-0/week0-azure-devops-001), [IIS](/learn/week-0/week0-iis-001).

## Sécurité — contrôler côté serveur

**Authentification et autorisation ?** La première établit l'identité, la seconde décide ce que
cette identité peut faire. Dans une API protégée, `401` indique qu'une authentification valable
manque ; `403` indique que l'accès est refusé malgré l'identité reconnue. Certaines applications
masquent volontairement l'existence d'une ressource avec un `404`.

**JWT, OAuth2 et OpenID Connect ?** JWT est un format de jeton ; décoder son contenu ne valide
pas sa signature. Un JWT signé n'est pas nécessairement chiffré.
OAuth2 encadre l'autorisation déléguée ; OpenID Connect ajoute l'authentification.
L'ID token est destiné au client pour l'identité ; l'access token est présenté à l'API.
Pour un client public interactif, connais le flux Authorization Code avec PKCE.
Ne construis pas ton propre fournisseur d'identité pour un exercice de préparation.

**Comment protéger une commande ?** Valide le jeton avec les bibliothèques appropriées
(signature, émetteur, audience et validité temporelle selon le contrat), puis vérifie l'autorisation
sur la ressource demandée. Modifier l'identifiant dans l'URL ne doit pas donner accès à la commande
d'un autre utilisateur. Validation des entrées, requêtes SQL paramétrées et messages d'erreur sobres
complètent ce contrôle. CORS est une politique de navigateur, pas un contrôle d'accès métier.

Relance : « Le bouton est caché dans Vue : est-ce suffisant ? » Non, le client peut appeler l'API
directement. Le serveur doit vérifier les droits à chaque opération protégée.
Support : [rôles et politiques](/learn/security-authorization-roles-policies-001).

## Zone sensible — parler de ton expérience avec précision

Prépare une présentation de deux minutes : ton rôle actuel, ce que tu développes effectivement,
une contribution concrète et ce que tu recherches dans cette mission.
N'ajoute pas de durée, technologie ou réalisation que tu ne peux pas détailler.

Pour une fonctionnalité livrée : besoin métier, critères d'acceptation, découpage, contribution
personnelle, tests, livraison et résultat observable. Pour un bug : symptôme, reproduction,
hypothèse, preuve, correctif, non-régression. Distingue ce que l'équipe a réalisé de ce que tu as fait.

Pour une technologie peu pratiquée, adapte cette formulation à la vérité : « Je ne l'ai pas utilisée
en production. J'ai travaillé ces notions et cet exercice. Voici ce que j'en comprends et le point
que je vérifierais avant de l'implémenter. » Une certification suivie n'est pas une certification
obtenue ; un essai local n'est pas une exploitation en production.

L'offre mentionne cinq ans d'expérience globale et deux sur la stack. Si l'écart est évoqué,
donne ta durée exacte et une réalisation précise. Aucun chiffre de productivité, migration réussie
ou expertise Azure n'est présumé ici.

Pour Scrum, prépare un exemple : critère d'acceptation ambigu clarifié, tâche découpée, blocage
signalé et échange avec l'équipe. La Definition of Done décrit les exigences communes pour
considérer un incrément terminé ; les critères d'acceptation précisent le comportement de la demande.
Une revue de code s'argumente par un risque, un exemple et une proposition vérifiable.

Questions utiles à poser : quelle part de back-end/front-end au quotidien ? Quels tests bloquent
la fusion ? Comment sont relues les spécifications ? Comment se passent déploiements et incidents ?
Quel accompagnement est prévu sur les technologies nouvelles pour la personne recrutée ?
Support : [carnet STAR](/career/career-star-workbook-001).

## Entretien blanc — 35 min sans réponses sous les yeux

Ce déroulé est un entraînement manuel. Il ne démarre aucun examen Forge.NET, ne collecte aucune
preuve et n'exécute aucun code à lui seul. Prépare la liste des questions, puis masque les fiches.

**Minutes 0 à 5 : présentation.** Deux minutes sur ton rôle et une contribution réelle.
Puis relance : « Qu'as-tu fait personnellement et comment as-tu vérifié le résultat ? »

**Minutes 5 à 15 : cinq questions, deux minutes chacune.**
Choisis une question de chaque groupe et demande au relecteur une relance concrète.

- C#/API : différence await/Result ; ou expliquer Scoped et le cas d'un DbContext injecté dans un singleton.
- Données/tests : détecter un N+1 ; ou choisir tests nominaux, frontières et invalides pour une règle.
- Vue : expliquer props/événements et computed ; ou gérer deux réponses HTTP arrivant dans le désordre.
- Distribution : traitement réussi mais acquittement perdu ; ou base enregistrée mais événement non publié.
- Livraison/sécurité : tests rouges avant déploiement ; ou accès à la commande d'un autre utilisateur.

**Minutes 15 à 30 : code à blanc.** Utilise
[Sélectionner les trois plus grands](/practice/csharp-linq-top-three-001), ou sa variante si la
solution a déjà été vue. Deux minutes pour reformuler et nommer les cas limites, dix pour coder,
trois pour vérifier et expliquer le coût. La méthode ne doit pas modifier l'entrée et doit
respecter les doublons. Les tests de l'exercice, pas cette page, déterminent le résultat exécuté.

**Minutes 30 à 35 : correction.** Classe chaque réponse « seul », « avec relance » ou
« à retravailler ». Pour le code, distingue compilation, cas vérifiés et cas seulement imaginés.
Retire trois actions précises : relire une durée de vie, réécrire une assertion, expliquer un doublon,
par exemple. Cette grille n'est ni un score de maîtrise ni un seuil de réussite au recrutement.

En cas de blocage pendant un vrai exercice : nomme l'hypothèse, réduis le problème à un exemple,
écris une solution simple et vérifie-la. Demande une précision si elle change le contrat.
Dire ce que tu vas vérifier vaut mieux qu'affirmer une garantie que tu ne peux pas défendre.

## Dernière fiche — cinq rappels avant l'échange

- Mon récit distingue mon travail de celui de l'équipe et cite une vérification réelle.
- Mon code traite un cas normal et une frontière ; je peux justifier la structure choisie.
- Une API vérifie les droits côté serveur, un DbContext a une portée adaptée et await ne crée pas automatiquement un thread.
- Une livraison de message peut se répéter ; une modification métier doit résister à ce rejeu.
- Je connais le prochain point à vérifier quand je ne sais pas, et j'annonce honnêtement les limites de ma pratique.

Retourne au [programme et à sa checklist finale](/prep/icube-plan-veille-001) pour répartir
le temps restant. Les lectures ci-dessous sont facultatives ; les repères nécessaires sont
présents dans ces fiches et dans les cours liés.

## Références officielles facultatives

- [Injection de dépendances dans ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0) : contrats et durées de vie.
- [Requêtes efficaces avec EF Core](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying) : projection, pagination et N+1.
- [Durée de vie du DbContext](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/) : unité de travail, portée et concurrence.
- [Tests unitaires .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices) : indépendance, assertions et structure.
- [Vue avec TypeScript et Composition API](https://vuejs.org/guide/typescript/composition-api) : props, événements et état typé.
- [Fondamentaux de la réactivité Vue](https://vuejs.org/guide/essentials/reactivity-fundamentals.html) : ref, objets réactifs et déstructuration.
- [Service Bus : transferts, verrous et acquittements](https://learn.microsoft.com/en-us/azure/service-bus-messaging/message-transfers-locks-settlement) : nouvelles livraisons et traitement des échecs.
- [Détection des doublons Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection) : déduplication des envois par identifiant et fenêtre.
- [Concepts Azure Pipelines](https://learn.microsoft.com/en-us/azure/devops/pipelines/get-started/key-pipelines-concepts?view=azure-devops) : stages, jobs, steps et artefacts.
- [Héberger ASP.NET Core avec IIS](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0) : modèles d'hébergement et configuration.
