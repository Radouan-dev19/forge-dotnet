# Programme sur 2 jours et demi : 5 h 10 pour préparer le technique

L'entretien RH est passé. Il reste deux jours et demi, avec un emploi à côté. Ce programme prévoit
**5 h 10** : deux soirées de 2 h, deux pauses de 20 min et 30 min le jour J. Les pauses de dix minutes
des soirées sont comprises. S7 ou S12 indiquent où trouver un sujet, pas une semaine à terminer.

Les priorités viennent de l'annonce ICube : C#/.NET, ASP.NET Core, EF Core, Vue 3/TypeScript,
Azure Service Bus, DevOps, IIS, tests et microservices. Le format exact du test reste inconnu.
L'offre distingue cinq ans d'expérience globale et deux ans sur les technologies.
L'objectif est de produire du code simple, d'expliquer tes décisions et de préciser ton expérience
réelle. Aucun dossier ne garantit une embauche.

Ouvre les [fiches techniques et l'entretien blanc](/prep/icube-scripts-entretien-001) uniquement
sur le sujet du créneau en cours. Elles servent de référence courte pendant le travail.

## Avant de commencer

Si le format n'a pas été communiqué, demande au recruteur : « Pour préparer au mieux notre échange,
pouvez-vous me préciser s'il comprendra du code en direct, une revue de code ou principalement des
questions techniques ? » C'est une demande à envoyer toi-même ; elle ne bloque pas ce programme.

Prépare un minuteur, ton IDE et une feuille « Mes trois hésitations ». Utilise un projet
d'entraînement distinct du code professionnel et des données réelles. Une installation qui bloque
plus de dix minutes ne doit pas absorber la soirée : utilise les exercices accessibles dans
Forge.NET ou un projet déjà fonctionnel.

Pour chaque créneau : lis cinq à dix minutes, ferme le support, produis quelque chose, puis explique
ta décision. Les manipulations locales et réponses orales de ce dossier ne sont ni collectées ni
validées automatiquement. Les exercices liés gardent leurs propres tests, indices, solutions,
variantes et règles de maîtrise. Si une solution est consultée, note cette aide et refais une
variante à blanc après une explication personnelle.

## Première soirée — 2 h : C#, API, données et tests

### Minutes 0 à 25 — C# et async

Pendant quinze minutes, tente [Composer un total de ligne](/practice/csharp-line-total-001).
Lis le contrat, écris les cas nominaux et limites, puis code sans solution. Respecte la signature,
les invariants et le contrat d'arrondi de l'exercice.

Pendant dix minutes, cible « Explication », « Contre-exemple » et « Entretien » dans
[Async simple, ordre et annulation](/learn/async-fundamentals-001). Explique ce que représente
une `Task`, ce que fait `await` pour une entrée-sortie en cours et pourquoi `.Result` bloque.
Une méthode `async` ne crée pas automatiquement un thread.

**À produire :** une tentative personnelle et une explication d'une minute. Si tu bloques sur une
boucle, une méthode ou un type, utilise la fin du créneau pour
[les méthodes et le contrôle de flux](/learn/csharp-control-methods-001). Note cette lacune.

### Minutes 25 à 55 — API et injection de dépendances

Lis dix minutes maximum [Contrôleurs et DTO](/learn/api-controllers-dtos-001) et
[Injection de dépendances et durées de vie](/learn/api-di-lifetimes-001).
Les cinq repères : route, DTO, validation, statut HTTP et service injecté.

Pendant vingt minutes, reproduis un POST de commande et un GET par identifiant dans ton projet
d'entraînement, avec une liste en mémoire. Si le projet n'est pas prêt, examine un endpoint du
[laboratoire API mini-ERP](/labs/api-mini-erp), puis réécris-le à blanc. Sa lecture est une aide,
pas une preuve d'autonomie.

Vérifie une création valide (`201`), une entrée invalide (`400`) et un identifiant absent
(`404`). Explique où placer la règle métier. Décris `Transient`, `Scoped` et `Singleton`
dans une API HTTP. Le stockage en mémoire sert à la répétition ; il ne démontre ni persistance
EF Core ni sécurité en concurrence.

**À produire :** deux endpoints expliqués, ou un endpoint réécrit et trois statuts justifiés.
Si ce geste est déjà acquis, cherche plutôt un oubli de validation, d'autorisation ou une
dépendance avec une durée de vie incorrecte.

### Minutes 55 à 65 — Pause

Éloigne-toi de l'écran. Reprends au créneau suivant même si l'API n'est pas parfaite.

### Minutes 65 à 90 — SQL et EF Core

Lis dix minutes [Jointures SQL](/learn/sql-joins-001) et
[EF Core : tracking, chargement et concurrence](/learn/ef-core-data-access-001).
Cible `INNER JOIN` contre `LEFT JOIN`, `DbContext`, migrations, tracking,
`AsNoTracking`, projection et N+1.

Pendant quinze minutes, écris une jointure entre `Customers(Id, Name)` et
`Orders(Id, CustomerId, Total)` qui conserve les clients sans commande. Explique les valeurs NULL
du côté commande. Écris ensuite une requête EF qui filtre avant de matérialiser, projette les
colonnes utiles et limite le résultat avec un ordre stable.

Si le SqlLab est déjà opérationnel, vérifie une jointure sur son jeu de données jetable.
Sinon, fais une revue de tes requêtes à voix haute ; leur exécution reste à vérifier.
Ne les exécute jamais sur la base de progression ou sur une base professionnelle.
[L'exercice EF de pagination](/practice/ef-keyset-pagination-001) servira si du temps se libère.

**À produire :** deux requêtes commentées et une réponse à « comment détecter des appels SQL
inutiles ? ». Un N+1 se constate dans les requêtes émises ; il ne découle pas automatiquement
de tout accès à une propriété de navigation.

### Minutes 90 à 110 — Tests

Lis cinq minutes [xUnit et Arrange Act Assert](/learn/tests-xunit-aaa-001).
Écris ensuite trois tests de la règle C# travaillée plus tôt : nominal, frontière et invalide,
selon son contrat. Utilise un résultat attendu explicite ; ne recalcule pas ce résultat avec
le même algorithme que le code testé.

Si les tests locaux ne démarrent pas, travaille les frontières avec
[Tester une règle de quantité](/practice/tests-quantity-rule-001), puis dis ce qui manque :
écrire une règle qui passe le runner n'est pas écrire une suite xUnit.
Le [laboratoire de stratégie de tests](/labs/testing-strategy) contient une référence de tests réels.

**À produire :** trois assertions expliquées et la différence entre règle pure et intégration
HTTP/base. Note si les tests ont réellement été lancés.

### Minutes 110 à 120 — Restitution sans notes

Explique le trajet : requête HTTP, validation, service métier, accès aux données, réponse.
Réponds à trois questions : pourquoi injecter une interface ? Pourquoi attendre une opération
asynchrone ? Que teste un cas limite ?

Note trois hésitations concrètes, par exemple « je ne sais pas expliquer Scoped ». Elles
détermineront la deuxième pause. Une difficulté précise est plus exploitable qu'un jugement
global sur ton niveau.

## Première pause disponible — 20 min : tes preuves professionnelles

Prépare deux récits vrais avec le [carnet STAR](/career/career-star-workbook-001) : une fonctionnalité
livrée et un bug corrigé. Consacre huit minutes à chacun : besoin, contribution personnelle,
difficulté, vérification, résultat. Dis chaque récit en deux minutes. Un résultat peut être
observable sans être chiffré.

Pendant les quatre dernières minutes, trouve un exemple de collaboration : clarification d'un
critère d'acceptation, retour de revue, estimation discutée ou blocage signalé dans l'équipe.
Aucun projet, migration, certification ni gain chiffré n'est présumé dans ce dossier.

## Deuxième soirée — 2 h : stack du poste et entretien blanc

### Minutes 0 à 30 — Vue 3 et TypeScript

Lis dix minutes [Vue.js 3 et TypeScript](/learn/week-0/week0-vue-typescript-001).
Retrouve `<script setup lang="ts">`, `ref`, `computed`, props et événements typés.
Ferme ensuite le guide et écris un composant : liste typée de commandes, filtre avec `v-model`,
liste filtrée avec `computed` et clé stable dans `v-for`.

Si un environnement Vue est prêt, lance-le ; sinon fais une revue ligne par ligne du brouillon.
Cette seconde option ne valide pas son exécution. Explique un appel API avec chargement, succès,
liste vide et erreur. Un type TypeScript ne valide pas à lui seul le JSON reçu.

**À produire :** un composant personnel ou un brouillon relu, avec son statut d'exécution annoncé.
Explique ce qui provoque le recalcul de `computed`.

### Minutes 30 à 50 — Service Bus et microservices

Lis les passages utiles de [Service Bus](/learn/week-0/week0-azure-service-bus-001) et
[Microservices](/learn/week-0/week0-microservices-001). Réserve cinq minutes à ce scénario oral :
une commande est enregistrée, un événement prévient un service de notification, ce service tombe
en panne puis redémarre.

Explique queue contre topic/abonnements, `PeekLock`, acquittement après traitement, réessais
bornés et file de lettres mortes. Si la base a été modifiée mais que l'acquittement échoue, une
nouvelle livraison doit être reconnue sans reproduire l'effet métier. La détection de doublons
à l'envoi ne remplace pas l'idempotence du consommateur.

**À produire :** deux minutes d'explication, avec un bénéfice et deux coûts des microservices.
Il n'est pas nécessaire de déployer plusieurs services pour cette répétition.

### Minutes 50 à 60 — Pause

Arrête la lecture dix minutes. Garde le créneau final pour la répétition à blanc.

### Minutes 60 à 75 — Azure DevOps, hébergement et IIS

Ouvre [Azure DevOps](/learn/week-0/week0-azure-devops-001) puis
[IIS](/learn/week-0/week0-iis-001). Cible pipeline, artefact, variables/secrets, environnements,
sites, pools et Hosting Bundle. La fiche technique résume aussi Key Vault, identité managée et logs.

Explique comment un commit devient une version déployée : build, tests, publication d'un artefact,
déploiement contrôlé et vérification de santé. Réponds à « cela fonctionne localement mais pas
sur le serveur : que regardes-tu ? » : statut exact, logs, runtime, configuration et droits.
Les logs ne doivent exposer ni jetons ni données sensibles.

**À produire :** le cycle de livraison en soixante secondes et trois vérifications d'incident.
Une lecture de pipeline n'est pas une expérience de déploiement Azure.

### Minutes 75 à 85 — Sécurité

Lis les repères de [l'authentification](/learn/security-authentication-001) et
[l'autorisation](/learn/security-authorization-roles-policies-001).
Utilise la fiche technique pour JWT, OAuth2 et OpenID Connect.

**À produire :** distinguer authentification/autorisation, expliquer `401`/`403` et dire pourquoi
la commande d'un autre utilisateur doit être protégée côté serveur, même si Vue masque le bouton.

### Minutes 85 à 120 — Entretien blanc de 35 min

Ouvre le [déroulé de l'entretien blanc](/prep/icube-scripts-entretien-001), puis cache les repères
de réponse. Fais cinq minutes de présentation, dix minutes de questions, quinze minutes de code
sans aide et cinq minutes de correction. Un collègue peut jouer l'interlocuteur ; sinon,
enregistre ta voix localement.

Pour le code, tente [Sélectionner les trois plus grands](/practice/csharp-linq-top-three-001)
si tu ne l'as pas encore fait. Le contrat conserve les doublons et ne modifie pas l'entrée :
reformule ces contraintes avant de coder. Si tu connais déjà la solution, choisis sa variante
publiée depuis la fiche.

**À produire :** une tentative chronométrée et trois corrections prioritaires. La grille de
répétition des fiches est une auto-évaluation, pas une prédiction d'embauche.

## Deuxième pause disponible — 20 min : corriger les hésitations

Consacre cinq minutes à chacune des trois hésitations : réponds sans notes, consulte le passage
manquant, referme-le et reformule avec un autre exemple. Les cinq dernières minutes servent à
résumer ton expérience et le trajet d'une requête.

Une solution d'exercice consultée garde ses conséquences dans Forge.NET : tentative non maîtrisée,
explication personnelle et révision à blanc planifiée. Ne transforme pas une lecture en résultat
de test ou en expérience de production.

## Jour J — 30 min : rappel et préparation pratique

- Dix minutes : les deux récits professionnels, ta présentation et une phrase précise sur une technologie peu pratiquée.
- Quinze minutes : cinq questions hésitantes, d'abord de mémoire ; DI, async, EF, doublons de messages et sécurité serveur.
- Cinq minutes : trajet ou connexion, matériel, heure exacte et environnement de code si demandé.

Garde une heure de coucher habituelle la veille. Juste avant l'échange, cesse les nouvelles lectures.
Pendant le code : reformuler, demander une précision utile, nommer les cas limites, proposer une
solution simple, coder, tester et expliquer une amélioration possible.

## Adapter le programme au temps et au format

**Seulement trois heures :** C#/API 45 min, SQL/EF 20 min, tests 15 min, Vue 20 min, Service Bus
15 min, livraison/sécurité 15 min, récits et entretien blanc 35 min, pauses 15 min.
Total : 180 min. Réduis la lecture avant de supprimer la tentative personnelle.

**Une heure supplémentaire :** deuxième répétition de 35 min, puis corrections pendant 25 min.
Si le C# reste bloquant, travaille plutôt deux exercices simples avec cas limites et explication.

**Code en direct confirmé :** remplace vingt minutes de lecture de la deuxième soirée par une
tentative dans le langage annoncé. Si le test est explicitement Vue/TypeScript, prends ces vingt
minutes sur SQL/EF et API pour faire fonctionner le composant.

**Architecture confirmée :** garde la première soirée ; prolonge Service Bus de quinze minutes
prises sur la rédaction du composant. Lis [les frontières de services](/learn-senior/senior-boundaries-001)
et [la messagerie entre services](/learn-senior/senior-messaging-001) pour données par service,
outbox et pannes. Reste capable de justifier aussi un monolithe modulaire.

Terraform, Kubernetes, Helm, Angular, React, Blazor et les algorithmes avancés passent après
les fondamentaux de l'annonce. Une précision du recruteur peut changer cet ordre.

## Checklist finale — observer ce que tu sais faire

Note sur ta feuille « seul », « avec aide » ou « à revoir ». Cette checklist n'est pas
enregistrée et ne produit aucune preuve de maîtrise.

- Expliquer ma contribution réelle à une fonctionnalité et à un bug.
- Écrire une méthode C# simple, respecter son contrat et vérifier une frontière.
- Expliquer interface, durée de vie de dépendance et appel avec await.
- Justifier DTO, validation et statuts d'une création ou d'une lecture HTTP.
- Commenter une jointure, une requête EF filtrée et un problème N+1.
- Écrire une assertion utile et distinguer test unitaire et intégration.
- Lire un composant Vue typé et suivre props, événements et état réactif.
- Raisonner sur un message livré deux fois et sur un consommateur en panne.
- Décrire pipeline, gestion des secrets et début d'un diagnostic IIS.
- Expliquer l'autorisation serveur et reconnaître une limite de mon expérience.

Reviens aux [fiches techniques](/prep/icube-scripts-entretien-001) pour les points « à revoir ».
Une case « avec aide » sert à répartir l'effort ; elle ne suffit pas pour se déclarer autonome.
