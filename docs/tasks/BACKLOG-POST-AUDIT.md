# Backlog post-audit — prompts d'exécution préparés

## 1. Statut

**Ce document n'est pas un incrément et ne porte aucun numéro.**

`docs/ROADMAP.md` est explicite : l'incrément 12 reste ouvert, son verdict est **REFUSÉ**, et « aucun
incrément suivant n'est créé implicitement ». Allouer ici les numéros 13 et suivants contournerait ce
cliquet. Ce document est donc une **préparation**, au sens de la section « Préparations éventuellement
parallèles » de `docs/tasks/README.md` : du travail rédigé d'avance, qui ne devient exécutable que par
une décision humaine explicite.

Pour convertir un élément de ce backlog en incrément numéroté, il faut : décider de lever ou de
contourner le refus de 12, allouer le numéro, créer la fiche au format des autres fiches de
`docs/tasks/`, et mettre à jour `NEXT_TASK.md`. Rien de tout cela n'est fait ici, volontairement.

`NEXT_TASK.md` n'est pas modifié : il continue de désigner l'incrément 12.

## 2. Objectif

Recenser, avec leur mesure, les défauts et manques constatés après la reprise éditoriale, puis fournir
pour chacun un **prompt d'exécution complet** — utilisable dans une session neuve, sans contexte
préalable.

Objectif produit visé par ce backlog : une plateforme auto-suffisante menant de junior à senior, ce
que les 24 semaines actuelles ne prétendent pas faire. `docs/CURRICULUM.md` renvoie lui-même les sujets
seniors à un « parcours distinct de 12–24 mois » qui n'existe pas.

## 3. Contexte à lire

`AGENTS.md`, `docs/CONTENT_AUTHORING_STANDARD.md`, `docs/MASTERY.md`, `docs/PEDAGOGICAL_AUDIT.md`,
`docs/ROADMAP.md`, `docs/CURRICULUM.md`.

## 4. Deux faits qui conditionnent tout ce backlog

1. **Le runner est C# uniquement.** `RunnerTypeCatalog`
   (`src/ForgeDotNet.CodeRunner/Container/RunnerSuiteContracts.cs`) ferme le catalogue de types à
   `bool, date, decimal, dictionary<string,int>, int, int[], list<int>, string`, et l'image ne
   référence pas `Microsoft.AspNetCore.*`. Elle référence en revanche toute la BCL, EF Core, SQLite et
   `Microsoft.Extensions.*`. Conséquence : un exercice peut faire de la cryptographie réelle, mais pas
   configurer un intergiciel ni exécuter du JavaScript.
2. **Le dépôt ne contient aucun JS/TS** — ni `package.json`, ni `tsconfig`. Le front-end part de zéro.

## 5. Triage mesuré

| # | Défaut | Mesure | Gravité |
|---|---|---|---|
| T1 | Accomplissements sans producteur | **21 sur 23** ; portes B/C/D fermées définitivement | **Bloquant** |
| T2 | Composante de score sans producteur | `Explanation` (10 %) → plafond 90 pour tous | Élevée |
| T3 | JWT / OAuth / OIDC | **0 mention** dans tout `content/` et `docs/` | **Élevée** |
| T4 | Laboratoires invisibles | 6 labs, **0 référence** dans `src/` | Élevée |
| T5 | Solutions de référence illisibles | **84/142** avec une ligne > 120 car., 36 > 200, record 414 | Élevée |
| T6 | Explication d'après-exercice maigre | médiane **112 mots**, 135/142 sous 200 (leçons : 1 400–2 000) | Élevée |
| T7 | Exercices hors banque d'examen | **43/142** jamais tirables | Moyenne |
| T8 | Densité de pratique | S1–S10 **8,8**/sem, S11–S17 **6,0**, S18–S20 **3,0**, S21–S24 **1,5** | Moyenne |
| T9 | Exercices à domaine trivial | **12** entièrement booléens (≤ 16 entrées possibles) | Moyenne |
| T10 | Manques REST | versionnage, `ETag`/`If-Match`, limitation de débit, `Cache-Control`, CORS, webhooks : 0 ou quasi | Moyenne |
| T11 | Fondamentaux distribués | `messaging` 0, `circuit breaker` 0, `outbox` 0, `résilience` 0 | Moyenne |
| T12 | Front-end | **0** JS/TS, **0** bUnit, Blazor jamais enseigné | **Élevée** |
| T13 | Revue de code et legacy | aucune, et `code-review` est une clé sans producteur | Élevée |
| T14 | Verdict d'audit | **REFUSÉ** : 7 personas non rejoués, aucun panel humain | Structurelle |

Commandes de reproduction de ces mesures, toutes en lecture seule :

```powershell
# T1 / T2
Select-String -Path tests/ForgeDotNet.UnitTests/ProjectAchievementTests.cs -Pattern 'MasteryPolicyCatalog\.'
Select-String -Path tests/ForgeDotNet.UnitTests/MasteryReachabilityTests.cs -Pattern 'UnproducedComponents'
# T3
Get-ChildItem content, docs -Recurse -File | Select-String -Pattern 'JWT|OAuth|OpenID|OIDC' | Measure-Object
# T4
Get-ChildItem src -Recurse -File | Select-String -Pattern 'content/labs' | Measure-Object
# T5
Get-ChildItem content/reference/exercises -Recurse -Filter Submission.cs |
    Where-Object { $_.Directory.Name -eq 'solution' } |
    Where-Object { (Get-Content $_.FullName | Measure-Object -Maximum Length).Maximum -gt 120 } |
    Measure-Object
```

## 6. Périmètre explicitement exclu de ce backlog

- Toute modification de `NEXT_TASK.md` ou de la matrice d'incréments de `docs/ROADMAP.md`.
- Toute levée du verdict de l'incrément 12 : elle exige les sept personas rejoués et un panel humain.
- Le rendu côté serveur, l'accessibilité poussée, la gestion d'état avancée et la livraison front —
  écartés du module front-end au profit des huit compétences de base (voir prompt 7).

---

## PROMPT 0 — Socle commun, à coller en tête de chaque prompt

```
Tu travailles sur Forge.NET, dépôt local à la racine du projet courant. Lis AGENTS.md et
docs/CONTENT_AUTHORING_STANDARD.md avant toute chose.

CONTRAINTES NON NÉGOCIABLES — les ignorer casse le build de façon non évidente :

1. RUNNER. Le bac à sable exécute uniquement `public static <T> Méthode(<scalaires>)` sur une classe
   `Submission`. Types autorisés, catalogue FERMÉ (src/ForgeDotNet.CodeRunner/Container/
   RunnerSuiteContracts.cs) : bool, date (DateOnly), decimal, dictionary<string,int>, int, int[],
   list<int>, string. Pas de Task, pas de long, pas de string[]. L'image référence la BCL complète,
   EF Core, SQLite et Microsoft.Extensions.* — mais PAS Microsoft.AspNetCore.*.
   Donc : System.Security.Cryptography, System.Text.Json et Convert sont disponibles.

2. DETTE ÉDITORIALE À ZÉRO. content/authoring/content-debt.json est vide et
   ContentAuthenticityTests plafonne à 0. Tout paragraphe de 12 mots ou plus partagé par plus de
   TROIS documents d'un même lot fait échouer le build. N'écris jamais deux fois la même phrase.
   Attention : les règles portent sur des paragraphes entiers, donc une phrase gabarit insérée au
   milieu d'un paragraphe échappe au validateur mais reste un défaut — ne le fais pas.

3. MARQUEURS INTERDITS dans tout contenu : `$mot`, `${`, `$(`, `{{`, TODO, FIXME, « à venir »,
   « change-me », et pour les fichiers d'exercice S11–S20 aussi : S21, S22, S23, S24,
   « Azure App Service », OpenTelemetry, Kubernetes. Ces refus sont testés.

4. APOSTROPHES. Le contenu hérité mélange U+2019, U+02BC et l'apostrophe ASCII. Si tu fais des
   remplacements par script, essaie les trois formes, sinon ils échoueront en silence.

5. ENCODAGE. UTF-8 SANS BOM, fins de ligne CRLF pour les .cs. `sed -i` casse les deux : préfère
   Edit/Write, ou réécris via [IO.File]::WriteAllText avec (New-Object Text.UTF8Encoding $false).
   Vérifie toujours avec `dotnet format --verify-no-changes` à la fin.

6. CONTRAT D'UN EXERCICE. Dossier content/reference/exercises/<id>/ avec exercise.json,
   statement.md, explanation.md, review-cards.md, starter/Submission.cs, solution/Submission.cs,
   tests/runner.json, tests/visible/cases.json (>= 3 cas), tests/hidden/cases.json (>= 4 cas).
   - exercise.json : 4 indices de niveaux 1..4, 4 erreurs fréquentes, complexity, variantId (doit
     pointer un exercice du même bloc de semaines), reviewCards, interviewQuestionId, 6
     reflectionFields dans l'ordre imposé.
   - Chaque cas déclare EXACTEMENT un `expected` OU un `expectedException`, jamais les deux.
   - Aucun cas ne répète les arguments d'un autre cas du même exercice.
   - Le starter contient `NotImplementedException`, la solution ne le contient pas.
   - Domaine d'entrée OUVERT : évite les signatures à deux booléens, mémorisables en table de vérité.
   - La solution de référence est LISIBLE : plusieurs lignes, une instruction par ligne, commentaires
     expliquant les décisions. Ne reproduis pas le style « tout sur une ligne » du contenu hérité.
   - explanation.md fait AU MOINS 350 mots et explique pourquoi, pas seulement comment.
   - Les 4 indices sont propres à l'exercice, aucun ne recopie une ligne de la solution
     (ExerciseHintQualityTests le vérifie).

7. CÂBLAGE OBLIGATOIRE d'un exercice neuf, sinon il est invisible ou fait échouer un test :
   - content/reference/curriculum/forge-reference.json : l'ajouter au module de sa semaine.
   - content/reference/reviews/exercise-review-cards.json : 2 cartes, questions UNIQUES dans toute
     la banque, 3 ou 4 options, une seule correcte, domaine = celui déduit de la première compétence
     par MasterySkillDomains. Obligatoire pour les domaines critiques (csharp, debugging, api, tests).
   - content/reference/interviews/interview-<id>.json : fiche d'entretien, critères et erreurs
     propres à l'exercice.
   - content/exams/<examen>/exam.json : l'ajouter à eligibleExerciseIds, sinon il ne sera JAMAIS tiré.
   - Compteurs figés à mettre à jour : ContentS11S20CoverageTests (matrice par semaine + totaux),
     ContentS21S24CoverageTests (nombre d'exercice.json, d'interviews, de fichiers, niveaux
     d'entretien), ContentCatalogLoadingTests (Items.Count, Exercise, InterviewQuestion),
     ReviewCardQualityTests (MinimumCoveredExercises).

8. VÉRIFICATION, à lancer avant de dire que c'est fini :
   dotnet test tests/ForgeDotNet.IntegrationTests --filter FullyQualifiedName~ExerciseCorrectnessTests
   powershell -ExecutionPolicy Bypass -File scripts/validate-content.ps1 content/reference
   dotnet test --nologo
   dotnet format --verify-no-changes
   Référence acceptée : 151 tests unitaires verts, 44 E2E verts, et EXACTEMENT 76 échecs
   d'intégration, tous dans SqlEfContentTests, DockerCodeRunnerSecurityTests,
   InitialCSharpContentTests, SqlLabSecurityTests, ExamSqlEfContentTests, ExamEfDockerRunnerTests,
   DebugLabDockerRunnerTests — ils exigent un démon Docker absent. Tout autre échec est une régression
   que tu dois corriger.

9. HONNÊTETÉ. Ce projet refuse de prétendre avoir vérifié ce qu'il n'a pas exécuté. Si tu ne peux pas
   prouver quelque chose, écris-le dans le rapport final. Ne monte jamais un plafond de test pour
   faire passer ton travail : les plafonds ne descendent que.

Ne commite pas. Termine par un rapport : ce qui est fait, ce qui est prouvé, ce qui reste ouvert.
```

---

## PROMPT 1 — T1 : produire les accomplissements manquants

```
[SOCLE COMMUN]

OBJECTIF. MasteryPolicyCatalog déclare 23 clés d'accomplissement pour les portes A à D.
ProjectAchievementTests en inventorie 21 SANS PRODUCTEUR : api-functional, ef-core,
validation-and-errors, unit-tests, integration-tests, clean-git, ten-minute-presentation, docker,
continuous-integration, authentication-authorization, logs, deployment, simulated-incident,
mock-interview, performance, security, pragmatic-architecture, autonomous-feature, code-review,
english, final-defense. Seuls project.console et exam.90-minutes produisent. Les portes B, C et D
sont donc fermées quel que soit le travail fourni : un apprenant lit « Porte B — bloquée » le dernier
jour comme le premier. C'est le défaut le plus grave du produit.

TRAVAIL.
1. Lis docs/MASTERY.md, src/ForgeDotNet.Domain/Mastery/MasteryPolicyCatalog.cs, les producteurs
   d'observations sous src/ForgeDotNet.Application/Mastery/, et ProjectAchievementTests.
2. Pour CHACUNE des 21 clés, classe-la dans exactement une catégorie et écris ta justification :
   (a) produisible dès maintenant à partir de preuves déjà collectées par le produit ;
   (b) produisible après un contenu qui n'existe pas encore — dis lequel ;
   (c) non produisible sans jugement humain — dis pourquoi.
3. Implémente TOUTES les clés de la catégorie (a). Pistes à vérifier, pas à supposer : unit-tests et
   integration-tests depuis les exercices `tests-*` réellement soumis au runner, api-functional depuis
   les exercices `api-*` plus l'examen api-security-v1, ef-core depuis les scénarios EF, docker et
   continuous-integration depuis les activités correspondantes.
4. RÈGLE ABSOLUE : un accomplissement ne s'attribue que sur une preuve VÉRIFIÉE côté serveur. Une
   déclaration manuelle reste visible et vaut zéro, exactement comme aujourd'hui pour la pratique.
   Ne relâche aucun seuil, aucun poids, aucun plafond d'aide.
5. Descends MaximumUnproducedKeys au nombre réellement atteint, et documente dans le commentaire XML
   quelles clés restent et dans quelle catégorie (b) ou (c) elles tombent.
6. Ajoute un test qui prouve, sur un profil fabriqué, qu'au moins une porte fermée aujourd'hui devient
   franchissable par le travail — sur le modèle de ce que MasteryRulesTests fait déjà pour la porte A.
7. Mets à jour docs/MASTERY.md et la section « Ce qui reste ouvert » de docs/PEDAGOGICAL_AUDIT.md.

CRITÈRE DE RÉUSSITE. Au moins une porte parmi B, C, D devient franchissable, prouvée par test. Les
clés restantes sont inventoriées et justifiées, pas ignorées.
```

---

## PROMPT 2 — T4 : rattacher les six laboratoires au produit

```
[SOCLE COMMUN]

OBJECTIF. content/labs/ contient six laboratoires — api-mini-erp, azure-operations, ci-delivery,
container-delivery, git-review, testing-strategy. Ce sont les SEULS endroits du dépôt avec un vrai
.csproj, un vrai contrôleur, un vrai Dockerfile durci, un vrai Bicep, un vrai workflow CI. Or
`grep -rn "content/labs" src/` ne renvoie RIEN : l'application ne les montre jamais. Un apprenant qui
suit le produit ne les rencontre pas, alors qu'ils portent toute la pratique réelle de Docker, de la
chaîne de livraison et d'une API complète.

TRAVAIL.
1. Étudie le chemin déjà fait pour les projets, qui est le modèle exact à suivre :
   content/reference/projects/<id>/project.json, le chargeur correspondant sous
   src/ForgeDotNet.Infrastructure/Content/, et les pages
   src/ForgeDotNet.Web/Components/Pages/ProjectsHome.razor et ProjectPage.razor.
2. Définis un manifeste lab.json par laboratoire — schéma v1 sous content/schemas/lab.schema.json,
   sur le modèle de project.schema.json : id, version, title, weeks, skills, prerequisites,
   estimatedMinutes, briefPath, objectifs vérifiables, commandes à exécuter, preuves attendues,
   limites annoncées, license.
3. Écris un brief par laboratoire s'il n'en a pas d'équivalent exploitable, en réutilisant ce qui
   existe déjà dans chaque dossier.
4. Ajoute la source de contenu, le service applicatif, et les pages /labs et /labs/{id}.
5. Le laboratoire déclare honnêtement ce qu'il prouve : il s'exécute chez l'apprenant, hors du bac à
   sable. Sa réussite est donc DÉCLARÉE, pas vérifiée par le serveur — sauf si le prompt 1 a branché
   un producteur d'accomplissement sur une preuve réellement collectable. Écris-le dans l'interface,
   ne le laisse pas deviner.
6. Tests : chargement du catalogue, rendu des deux pages, refus d'un manifeste invalide. Mets à jour
   les compteurs de ContentCatalogLoadingTests et ContentS21S24CoverageTests.
7. Mets à jour README.md — la ligne « Routes disponibles » et la limite qui dit que les labs ne sont
   rattachés à rien.

CRITÈRE DE RÉUSSITE. Les six laboratoires sont accessibles depuis l'application, avec ce qu'ils
prouvent et ce qu'ils ne prouvent pas écrit noir sur blanc.
```

---

## PROMPT 3 — T3 : JWT, dans le parcours (S14)

```
[SOCLE COMMUN]

OBJECTIF. JWT, OAuth, OIDC : ZÉRO mention dans tout content/ et docs/. Le seul exercice « jeton »,
security-bearer-header-001, valide la chaîne « Bearer <valeur> » et ne regarde jamais le jeton. La
leçon security-authentication-001 traite le mot de passe. Le laboratoire api-mini-erp s'authentifie
par clé d'API. Un apprenant termine les 24 semaines sans avoir jamais lu, validé ni émis un jeton —
c'est le plus gros écart entre le parcours et les offres .NET.

PRINCIPE DE CONCEPTION. L'exercice enseigne le MÉCANISME, le laboratoire enseigne le CÂBLAGE. Le
runner n'a pas Microsoft.AspNetCore.*, mais il a toute la BCL : HMACSHA256,
CryptographicOperations.FixedTimeEquals, Convert.FromBase64String, System.Text.Json. Valider un jeton
à la main est donc réellement faisable, sur domaine ouvert, et non contournable. C'est aussi meilleure
pédagogie : on apprend ce que l'intergiciel fait avant de le configurer.

TRAVAIL.
1. Deux leçons au standard des 14 sections. Copie la structure de
   content/reference/curriculum/lessons/ef-core-data-access-001/lesson.md, qui est la référence de
   qualité du dépôt : contre-exemple montrant le code fautif PUIS sa correction, quiz, section
   entretien.
   - security-jwt-anatomy-001 : trois segments, Base64Url et son remplissage, et le point que presque
     personne ne dit — un JWT n'est PAS chiffré, tout le monde lit la charge utile. Signature contre
     chiffrement. Revendications iss/aud/exp/nbf/iat/sub/jti.
   - security-jwt-validation-001 : l'ordre de validation et pourquoi il n'est pas négociable —
     signature AVANT revendications ; l'algorithme est imposé par le serveur, jamais lu dans
     l'en-tête ; tolérance d'horloge ; aud et iss obligatoires ; un jeton ne se révoque pas, d'où la
     durée courte et le couple accès/rafraîchissement.
2. Six exercices, semaine 14, compétences security.authentication ou security.token :
   security-jwt-decode-001      string ReadClaim(string token, string claim)
   security-jwt-signature-001   bool IsSignatureValid(string token, string secret)
   security-jwt-lifetime-001    string LifetimeState(string token, int nowUnix, int skewSeconds)
   security-jwt-audience-001    bool IsIntendedFor(string token, string audience, string issuer)
   security-jwt-alg-none-001    string RejectionReason(string token, string expectedAlg)
   security-jwt-refresh-001     string RefreshDecision(string token, int nowUnix, int windowSeconds)
   Chacun couvre en cas cachés : signature falsifiée, segment tronqué, remplissage Base64Url absent,
   revendication absente, borne exacte. Les jetons de test sont fabriqués à la main avec un secret
   factice écrit dans l'énoncé — aucune valeur sensible réelle. security-jwt-alg-none-001 doit couvrir
   alg:none ET la confusion d'algorithme, qui sont deux attaques distinctes.
3. Un laboratoire content/labs/api-jwt-bearer/ : vrai .csproj ASP.NET Core, AddAuthentication()
   .AddJwtBearer(...), politiques d'autorisation, [Authorize] par portée, et une suite
   WebApplicationFactory prouvant 401 sans jeton, 401 sur jeton expiré, 403 sur portée insuffisante,
   200 sur jeton valide. Clé de signature factice en configuration locale, aucune dépendance réseau.
   Si le prompt 2 est fait, déclare-le dans lab.json.
4. Câblage complet des six exercices selon le point 7 du socle. Banque d'examen : api-security-v1.
5. S14 passe de 6 à 12 activités : mets à jour la matrice figée de ContentS11S20CoverageTests et tous
   les totaux qui en dépendent.

CRITÈRE DE RÉUSSITE. ExerciseCorrectnessTests prouve hors Docker que les six solutions passent tous
leurs cas cachés et que chaque starter en échoue au moins un — donc que la vérification
cryptographique est réellement correcte. Le laboratoire a sa suite verte, lancée séparément.
```

---

## PROMPT 4 — T5 + T6 : rendre les solutions lisibles et les explications utiles

```
[SOCLE COMMUN]

OBJECTIF. Deux défauts de contenu que ni les schémas, ni les règles d'authenticité, ni dotnet format
ne voient, parce que ces fichiers sont du contenu et non du code compilé par le produit.

DÉFAUT 1 — solutions illisibles. 84 des 142 solutions de référence ont une ligne de plus de 120
caractères, 36 dépassent 200, le record est à 414 (csharp-json-number-count-001). Toute la logique est
écrite sur une seule ligne physique. Or docs/MASTERY.md est sans ambiguïté : consulter une solution met
la pratique autonome de cet exercice À ZÉRO, définitivement. L'apprenant paie donc le prix maximal pour
lire un artefact écrit dans un style qu'aucune revue n'accepterait.

DÉFAUT 2 — explications maigres. explanation.md fait 112 mots de médiane et 135 exercices sur 142 sont
sous 200 mots, alors qu'une leçon en fait 1 400 à 2 000. C'est la boucle de retour juste après
l'effort, et 45 % du score porte sur la pratique.

TRAVAIL.
1. Reformate les solutions : une instruction par ligne, accolades explicites, noms conservés à
   l'identique, un commentaire par décision non évidente. NE CHANGE AUCUN COMPORTEMENT :
   ExerciseCorrectnessTests doit rester vert sur les 142 exercices, c'est ta preuve de non-régression.
   Traite par lots de 20 et relance le test à chaque lot.
2. Ajoute un test de contenu qui fige une longueur de ligne maximale de 120 caractères dans
   solution/Submission.cs et starter/Submission.cs, avec un plafond à cliquet qui ne peut que
   descendre — sur le modèle de ContentAuthenticityTests. Documente pourquoi cette règle existe.
3. Étoffe les 135 explanation.md sous 200 mots jusqu'à 350 mots minimum. Le contenu attendu n'est pas
   une paraphrase de la solution : pourquoi cette approche plutôt qu'une autre, quelle erreur le cas
   caché réfute, quel coût, et quelle décision se transpose ailleurs. Étalon :
   content/reference/exercises/api-validation-aggregate-001/explanation.md.
   Interdit : réutiliser une phrase d'un autre exercice — la dette est à zéro.
4. Travaille par famille (algo, csharp, structures, debug, api, tests, security, quality, git, docker,
   ci, azure) et relance validate-content.ps1 après chaque famille.

CRITÈRE DE RÉUSSITE. Aucune ligne de solution au-delà de 120 caractères, aucune explication sous 350
mots, ExerciseCorrectnessTests vert sur 142 exercices, validateur à 0 erreur.
```

---

## PROMPT 5 — T3 suite : OAuth 2.0 et OIDC, hors ligne

```
[SOCLE COMMUN] — fais le prompt 3 avant celui-ci.

OBJECTIF. Ajouter OAuth 2.0 et OpenID Connect. Contrainte d'auto-suffisance : AUCUN fournisseur réel,
aucune dépendance réseau. Le module utilise un serveur d'autorisation factice EN PROCESSUS, ce qui est
à la fois hors ligne et meilleur pédagogiquement — on voit la mécanique au lieu de la configurer.

TRAVAIL.
1. Trois leçons :
   - Les flux et lequel choisir : code d'autorisation avec PKCE pour tout client public, identifiants
     client pour un service, et pourquoi les flux implicite et mot-de-passe sont morts.
   - Code d'autorisation avec PKCE : le rôle de code_verifier et code_challenge, ce que `state`
     protège (le rejeu de requête inter-site) et ce que `nonce` protège (le rejeu d'id_token) — deux
     choses distinctes que presque personne ne sépare correctement en entretien.
   - OAuth n'est pas OIDC : autorisation déléguée contre identité. Ce qu'un jeton d'accès n'est pas —
     ce n'est pas une preuve d'identité, et l'utiliser comme telle est une faille classique.
     Rafraîchissement, rotation, révocation.
2. Cinq exercices runner, domaine ouvert :
   - vérification d'un défi PKCE S256 (SHA-256 + Base64Url, tout est dans la BCL) ;
   - validation d'un `state` contre le rejeu et l'absence ;
   - choix du flux à partir des propriétés du client — attention, ne fais pas de cet exercice une
     table de vérité booléenne : passe les propriétés en chaîne descriptive pour garder un domaine
     ouvert ;
   - validation d'un id_token OIDC : nonce, azp, at_hash ;
   - calcul d'une fenêtre de rotation de jeton de rafraîchissement.
3. Un laboratoire content/labs/oauth-local-idp/ : serveur d'autorisation minimal en processus, les
   deux flux de bout en bout, et des tests prouvant qu'un code_verifier faux est refusé, qu'un `state`
   rejoué est refusé, et qu'un jeton d'accès présenté comme id_token est refusé.
4. Câblage complet, banque d'examen api-security-v1, compteurs figés.

PLACEMENT. Ce contenu est de niveau confirmé/senior. Ne le fais pas entrer de force dans les 24
semaines : place-le dans la piste senior du prompt 8 si elle existe déjà, sinon en S21 dont la
compétence azure.identity le rend cohérent — l'identité gérée EST un flux d'identifiants client, et le
dire explicitement relie les deux.
```

---

## PROMPT 6 — T10 : combler les six manques REST

```
[SOCLE COMMUN]

OBJECTIF. La partie API est solide sur les bases — sémantique HTTP, routage, DTO, DI, validation avec
ProblemDetails, async/annulation, pagination y compris par curseur, tri en liste blanche, négociation
de contenu, OpenAPI, idempotence. Mesure des manques : versionnage d'API quasi nul, ETag et If-Match
0, limitation de débit 0, Cache-Control 0, CORS 1 fichier, webhooks 0. Tous reviennent en entretien.

TRAVAIL. Une leçon et deux exercices par sujet.
1. Versionnage : par URL, par en-tête, par type de média ; ce qu'est un changement cassant ; comment
   retirer une version.
2. ETag et If-Match : c'est le PENDANT HTTP de la concurrence optimiste déjà enseignée par
   sql-isolation-001 et ef-core-data-access-001. Fais le lien explicitement — cette continuité est
   rare et vaut d'être montrée. Exercices : calcul d'un ETag stable, décision 200/304/412 selon
   If-None-Match et If-Match.
3. Limitation de débit : fenêtre fixe contre fenêtre glissante contre jeton de seau, en-têtes
   Retry-After et RateLimit-*, et pourquoi limiter par identité plutôt que par adresse.
4. Cache-Control : public/private, max-age, no-store, revalidation ; ce qui ne doit jamais être mis en
   cache et pourquoi.
5. CORS : préflight, ce qu'un en-tête autorise réellement, et pourquoi un joker avec identifiants est
   refusé par la spécification.
6. Webhooks : signature HMAC de la charge utile, horodatage et fenêtre anti-rejeu, tolérance
   d'horloge. Excellent exercice de crypto, et réutilise ce qui a été appris au prompt 3.

Câblage complet, compteurs figés. Répartis les activités sur S11 à S13, qui sont à 6 par semaine
contre 8,8 en S1–S10 — ça sert aussi T8.
```

---

## PROMPT 7 — T12 : front-end Angular, React et Blazor

```
[SOCLE COMMUN]

OBJECTIF. Trois frameworks, niveau « trouver un emploi et le garder » : savoir construire, tester,
brancher sur une API authentifiée, et se débrouiller dans une base existante. Le dépôt part de zéro :
aucun package.json, aucun tsconfig, aucun bUnit.

STRATÉGIE ARRÊTÉE — mixte. Ne la remets pas en question, elle a été décidée.
  - Le raisonnement front-end transposable est enseigné par des exercices C# à domaine ouvert, qui
    passent par le runner existant et ALIMENTENT donc le score de maîtrise.
  - Le câblage réel passe par des laboratoires, un par framework.
  - Blazor est corrigé automatiquement via bUnit — simple paquet NuGet, aucun problème hors ligne — et
    produit donc une vraie preuve serveur.

PROFONDEUR ARRÊTÉE — les 8 compétences pour chacun des trois frameworks. Niveau visé : opérationnel
sur celui de l'équipe, capable de se débrouiller sur les deux autres. Les huit : composant et cycle de
vie ; état local contre état partagé ; formulaire et validation ; appel HTTP, erreurs et annulation ;
routage et garde d'accès ; consommation d'un JWT et rafraîchissement silencieux ; test de composant ;
construction et déploiement d'un artefact.

TRAVAIL.
1. Quatre leçons de socle, indépendantes du framework — enseignées UNE fois : rendu et réconciliation,
   état et flux de données unidirectionnel, formulaires contrôlés et validation, contrat
   client/serveur vu du client (erreurs, annulation, jeton).
2. Trois leçons de spécificité, une par framework, qui disent ce que CE framework fait différemment :
   - Angular : injection de dépendances, RxJS et le désabonnement — la fuite la plus fréquente —,
     détection de changement, formulaires réactifs, intercepteur HTTP pour le jeton, garde de route.
   - React : hooks et leurs règles, dépendances d'effet, état dérivé, contexte contre magasin externe,
     et l'erreur la plus fréquente : l'effet qui se redéclenche en boucle.
   - Blazor : Server contre WebAssembly et ce que le choix implique vraiment, paramètres et cascade,
     interop JS, cycle de rendu, AuthenticationStateProvider. Atout à exploiter : le produit lui-même
     est du Blazor Server — 32 fichiers .razor, interop JS déjà présente dans
     src/ForgeDotNet.Web/Components/Pages/PracticePage.razor. Sers-t'en comme exemple réel plutôt que
     d'inventer un exemple.
3. Quatre exercices C#, qui comptent au score. Respecte le catalogue de types et garde des domaines
   d'entrée OUVERTS :
   front-state-reducer-001      string Reduce(string state, string actions)
     réduction d'état, immutabilité, action inconnue ignorée contre refusée, ordre d'application
   front-form-field-state-001   string FieldState(string field, string interactions)
     machine à états pristine/dirty/touched/invalid — sémantique réelle des formulaires Angular et
     React, distincte de la validation serveur d'api-validation-aggregate-001
   front-cache-decision-001     string CacheDecision(string entry, int nowSeconds, int staleAfter, int expireAfter)
     frais, périmé mais servable pendant revalidation, expiré : sémantique stale-while-revalidate
   front-route-guard-001        string GuardDecision(string token, string requiredScope, int nowUnix, string currentPath)
     autorisé, redirection vers connexion en préservant l'URL de retour, interdit — consomme le prompt 3
   ATTENTION : front-form-field-state-001 ne doit pas recouvrir api-validation-aggregate-001. Le
   premier porte sur l'état d'interaction d'un champ côté client, le second sur l'agrégation des
   violations côté serveur. Lis le second avant d'écrire le premier.
4. Trois laboratoires, chacun consommant l'API du laboratoire api-jwt-bearer du prompt 3 :
   - content/labs/blazor-jwt-client/ : vrai projet, garde de route, AuthenticationStateProvider lisant
     le JWT, et une suite bUnit prouvant le rendu authentifié, le refus non authentifié et
     l'annulation d'un appel en cours. Ajoute bUnit à Directory.Packages.props.
     PREUVE SERVEUR : branche un producteur d'accomplissement si le prompt 1 est fait.
   - content/labs/angular-orders-client/ et content/labs/react-orders-client/ : vrais projets avec
     leur suite, versions ÉPINGLÉES et fichier de verrouillage commité.
5. Sur la contrainte hors ligne, sois honnête plutôt que malin. Le projet promet « aucune dépendance
   réseau obligatoire », et un laboratoire npm rompt cette promesse. Ne prétends pas le contraire et ne
   verse pas node_modules dans le dépôt. Écris dans le README de chaque laboratoire JS, et dans sa page
   produit : ce laboratoire exige UNE installation réseau initiale, c'est le seul endroit du parcours où
   c'est vrai, les versions sont épinglées, et voici la commande exacte. Mets à jour la section
   « Limites actuelles » du README.md racine.
6. Trois activités « base existante », une par framework : un client de quelques milliers de lignes
   avec un défaut planté à trouver, par la méthode en quatre temps des DebugLabs appliquée à du code que
   l'apprenant n'a pas écrit. C'est la seule façon d'entraîner la navigation en terrain inconnu, que le
   parcours ne fait jamais puisque tout part d'un starter vide.
7. Déclare dans chaque laboratoire ce qui est prouvé par le serveur et ce qui ne l'est pas. Blazor :
   preuve serveur. Angular et React : preuve déclarée.

PLACEMENT. Le front-end est un axe nouveau, pas un ajout dans une semaine existante. Crée un bloc
dédié et mets à jour docs/CURRICULUM.md, qui ne mentionne aujourd'hui aucun front-end.
```

---

## PROMPT 8 — T11 + T13 : piste senior S25 à S32

```
[SOCLE COMMUN]

OBJECTIF. docs/CURRICULUM.md déclare lui-même que les 24 semaines ne mènent pas au niveau senior : sa
dernière section renvoie systèmes distribués, messaging, cache, résilience, observabilité avancée,
architecture, mentoring, estimation et incidents à un « parcours distinct de 12–24 mois » qui n'existe
pas. Mesure : messaging 0, message broker 0, saga 0, circuit breaker 0, résilience 0, Polly 0, gRPC 0,
outbox 0, cohérence éventuelle 0. Le mot « microservice » apparaît UNE fois, dans la leçon qui explique
pourquoi le projet final n'en est pas un.

CADRAGE À RESPECTER. Cette position n'est pas un oubli, c'est une thèse défendable : un junior qui
découpe en microservices produit un système distribué qu'il ne sait pas exploiter. Ne la renverse pas.
L'objectif n'est pas d'apprendre à faire des microservices, c'est d'acquérir les fondamentaux
distribués qui rendent la conversation crédible, y compris le refus argumenté de découper — ce que
sondent réellement les entretiens seniors.

STRUCTURE. Crée un SECOND document de curriculum,
content/reference/curriculum/forge-senior-reference.json, plutôt que d'étendre le premier :
ContentS21S24CoverageTests fige weeks = 24 et 24 modules, et cette matrice a de la valeur comme relevé
du parcours junior→confirmé. Prévois le chargement, la page, et les tests de couverture propres.

S25  résilience d'un appel : délai, réessai avec jitter, disjoncteur, cloisonnement
     -> budget d'appel tenu sous panne injectée
S26  clés d'idempotence, rejeu, au-moins-une-fois contre exactement-une-fois
     -> endpoint rejouable prouvé
S27  messagerie : consommateur idempotent, outbox, file de lettres mortes
     -> deux services communicants, hors ligne
S28  cohérence éventuelle, compensation, ce que la transaction ne couvre plus
     -> scénario de compensation
S29  découper ou non : coût d'un déployable, frontières, REFUS argumenté
     -> note de décision contradictoire
S30  observabilité distribuée : corrélation inter-service, budget d'erreur
     -> incident distribué résolu
S31  revue de code
     -> revue notée, produit l'accomplissement code-review
S32  base de code existante
     -> correctif + non-régression sur du legacy

S31 et S32 sont la réponse la plus utile à la contrainte d'auto-suffisance, et méritent un soin
particulier. Les deux trous les plus coûteux du parcours sont « aucune revue humaine » et « aucune base
de code existante ». On ne fabrique pas un relecteur humain, mais on peut fabriquer :
  - S31 : des diffs VOLONTAIREMENT DÉFECTUEUX à réviser, dont les défauts sont connus du système, donc
    notables automatiquement. L'apprenant soumet ses remarques classées par gravité ; le score compare
    aux défauts plantés. Cela entraîne la compétence de relecture et produit code-review, qui n'a aucun
    producteur aujourd'hui. Plante des défauts de plusieurs natures : correction, sécurité, concurrence,
    et un faux positif — une remarque de style présentée comme bloquante doit coûter.
  - S32 : une base de quelques milliers de lignes avec un défaut planté, à trouver par la méthode en
    quatre temps des DebugLabs, appliquée cette fois à du code que l'apprenant n'a pas écrit.

À DÉCLARER EXPLICITEMENT, sans l'enjoliver : recevoir une revue d'un humain qui n'est pas d'accord,
arbitrer sous pression, un désaccord d'équipe — aucun contenu ne les remplace. Écris-le dans
docs/CURRICULUM.md et dans la page de la piste. C'est la discipline que ce projet s'impose partout
ailleurs, elle doit valoir ici aussi.
```

---

## PROMPT 9 — T7 + T8 + T9 : couverture d'examen, densité, exercices triviaux

```
[SOCLE COMMUN]

OBJECTIF. Trois défauts de volume et de qualité qui se traitent ensemble.

T7. 43 exercices sur 142 ne figurent dans aucune banque d'examen : csharp 12, structures 11, debug 10,
algo 4, api 2, git 2, ci 1, docker 1. Un exercice hors banque n'alimente jamais la composante « examen
sans aide », soit 25 % du score. La cause structurelle a déjà été corrigée : les listes d'éligibilité
étaient plafonnées à 16 entrées et la banque entière était refusée au démarrage au-delà, ce qui gelait
toute croissance — voir ExamBankBoundsTests.

T8. Densité : S1–S10 à 8,8 exercices par semaine, S11–S17 à 6,0, S18–S20 à 3,0, S21–S24 à 1,5. La
cible annoncée est 8 sur S11–S17, soit 14 exercices à ajouter.

T9. Douze exercices ont un domaine d'entrée entièrement booléen, donc au plus 16 entrées possibles :
api-di-lifetime-choice-001, api-http-status-map-001, azure-hosting-decision-001,
azure-incident-brief-001, azure-release-evidence-001, azure-secret-source-001, ci-deploy-gate-001,
ci-job-result-001, docker-hardening-policy-001, quality-review-severity-001,
security-login-message-001, tests-double-choice-001. Une table de vérité à quatre lignes n'entraîne
pas le geste annoncé, elle entraîne la lecture de l'énoncé.

TRAVAIL.
1. Répartis les 43 exercices orphelins dans les banques d'examen dont le thème correspond, en
   respectant drawCount et la borne de 256. Mets à jour les compteurs de ContentS11S20CoverageTests.
2. Ajoute 14 exercices sur S11–S17 pour atteindre 8 par semaine, puis porte S18–S20 de 3 à 6. Suis le
   modèle des six exercices du lot 1 — api-validation-aggregate-001, api-sort-expression-001,
   security-scope-grant-001, tests-boundary-probe-001, tests-shared-state-leak-001,
   quality-unreachable-branch-001 — dont le principe de conception est le domaine d'entrée ouvert.
3. Pour les douze exercices triviaux : ne les supprime pas, ils portent une décision qui a sa valeur.
   Ajoute à côté de chacun un exercice frère à domaine ouvert sur le même sujet, et fais pointer le
   variantId de l'un vers l'autre. Exemple : à côté de docker-memory-limit-001, un exercice qui analyse
   une chaîne de contraintes de ressources et rend la limite effective avec la raison du rabattement.
4. Pour Docker, CI et Azure, rappelle-toi que le runner ne peut pas héberger le geste réel : le
   véhicule est le laboratoire. Ne fabrique pas d'autres fonctions pures déguisées.
```

---

## 7. Ordre recommandé

**1 → 2 → 3 → 4 → 6 → 7 → 5 → 9 → 8**

Le prompt 1 débloque la récompense du travail, sans quoi tout le reste s'ajoute à un socle qui ne sait
rien attribuer. Le 2 rend visible la seule pratique réelle existante et conditionne tout laboratoire
neuf. Le 3 est le plus gros gain d'employabilité par heure investie, et le 7 en dépend — la garde de
route front-end consomme le JWT du prompt 3. Le 4 est peu coûteux et améliore 142 exercices d'un coup.
Le 8 vient en dernier parce qu'il crée une piste entière.

## 8. Vérification transverse

Après chaque prompt, la même chaîne et la même référence acceptée : 151 tests unitaires verts, 44 E2E
verts, 76 échecs d'intégration Docker et **aucun autre**, `dotnet format` propre, validateur à 0 erreur
sur `content/reference`, `content/sql` et `content/fixtures/valid`, registre de dette toujours à 0.

Un seul écart à cette référence est une régression, pas un effet de bord acceptable.
