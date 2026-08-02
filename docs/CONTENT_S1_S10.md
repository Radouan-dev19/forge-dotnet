# Contenu S1–S10 — matrice gelée

## Portée et règle de comptage

Cette matrice est la source de vérité de l’incrément 08. Un élément compte seulement si son manifeste est valide, ses références sont résolues, ses fichiers privés existent et ses preuves spécialisées sont vertes. Les fixtures de validation ne comptent jamais. Les deux exercices historiques `reference-total-*` comptent après remplacement de leurs README de démonstration par des starters, solutions et tests exécutables.

Les minima globaux C#/algo, DebugLab et SQL/EF doivent être atteints ici : aucun incrément S11+ n’est autorisé à réparer un trou S1–S10. Les 190 questions d’entretien et 50 cartes d’anglais restent, conformément à la fiche 10, un volume final ; 08 ne crée que les questions et cartes directement liées à ses exercices et leçons.

## Volumes cibles

| Semaine | Leçons | C#/algo | Debug | SQL/EF | Mini-projets | Examen de jalon |
|---:|---:|---:|---:|---:|---:|---|
| S1 | 3 | 10 | 4 | 0 | 0 | — |
| S2 | 3 | 10 | 3 | 0 | 1 | Examen 1 |
| S3 | 3 | 10 | 4 | 0 | 0 | — |
| S4 | 3 | 14 | 3 | 0 | 1 | Examen 2 |
| S5 | 3 | 14 | 3 | 0 | 1 | — |
| S6 | 3 | 15 | 3 | 0 | 0 | — |
| S7 | 3 | 12 | 5 | 0 | 1 | Examen 3 |
| S8 | 3 | 0 | 0 | 14 | 0 | — |
| S9 | 3 | 0 | 0 | 13 | 0 | — |
| S10 | 3 | 0 | 0 | 13 | 1 | Examen 4 |
| **Total** | **30** | **85** | **25** | **40** | **5** | **4** |

## Compétences obligatoires

| Semaine | Couverture sans trou | Preuve principale |
|---:|---|---|
| S1 | types, conversions, contrôle, méthodes, I/O locale, breakpoints | programmes courts et DebugLab |
| S2 | tableaux, listes, dictionnaires, chaînes, dates, cas limites | transformations testées et bibliothèque de collections |
| S3 | classes, encapsulation, interfaces, composition, exceptions, nullable | modèle métier et tests |
| S4 | génériques, delegates, lambdas, LINQ, fichiers, JSON | import de commandes |
| S5 | reformulation, pseudocode, complexité, recherche, tris simples | série algorithmique expliquée et moteur de promotions |
| S6 | piles, files, dictionnaires, récursivité utile, arbres minimaux | exercices de structures et explications |
| S7 | stack traces, breakpoints avancés, inspection des données, async simple | cinq familles de bugs et analyseur de logs |
| S8 | modèle relationnel, contraintes, SELECT, filtres, jointures | résultats SQL déterministes |
| S9 | agrégations, sous-requêtes, CTE, transactions, isolation | effets et resets contrôlés |
| S10 | index, plans, pagination, EF Core, tracking, N+1, concurrence | module données mini-ERP |

## Lots révisables

- L01–L06 : six lots de cinq leçons, chacun validé séparément.
- C01–C15 : quinze lots de cinq exercices C#/algo ajoutés ou durcis ; les dix exercices 04D forment deux lots de base.
- D01–D04 : quatre lots de quatre à cinq DebugLabs, en plus des huit scénarios initiaux relus.
- Q01–Q06 : six lots de quatre à cinq scénarios SQL/EF, en plus des douze scénarios initiaux relus.
- P01 : cinq mini-projets, sans solution complète avant soumission.
- E01 : examens 1–4, banques séparées et sans aide.

Un lot est refusé si un item du lot est ambigu, dupliqué, incomplet, non exécutable ou si une vérification applicable échoue. Les commits proposés dans le rapport final suivent ces frontières ; l’agent n’effectue aucun commit sans demande explicite.

## Frontière S11+

La matrice ne contient ni HTTP/API, ni authentification, ni xUnit comme sujet de cours, ni Git, Docker, CI, Azure, carrière ou projet final. Une occurrence technique dans l’infrastructure de validation ne constitue pas du contenu apprenant.

## État de validation au 30 juillet 2026

- Matérialisés et validés : 30 leçons, 85 exercices C#/algo, 25 DebugLabs, 40 scénarios SQL/EF, 84 questions d’entretien liées, 5 mini-projets et les examens 1–4.
- Preuves vertes : schémas/catalogue, lecture des 30 leçons, 85 solutions et starters dans le runner Docker, 25 scénarios cassés/corrigés, 40 SQL/EF avec isolation/reset, quatre banques d’examen et formatage.
- L’examen 4 contient six soumissions SQL et deux soumissions EF Core, toutes tirées. Chaque starter SQL échoue et chaque solution passe dans une session SqlLab jetable ; chaque starter EF échoue et chaque solution passe dans le runner Docker avec SQLite en mémoire.
- La délégation est typée et figée dans le snapshot d’examen : SQL va uniquement vers `SqlLabExamRunner`, C#/EF vers `ICodeRunner`. Les attentes SQL, solutions et cas cachés restent serveur ; un runner indisponible ne produit ni réussite ni preuve.
- L’image runner étendue a repassé les 18 tests d’abus et un scan Trivy hors ligne : aucune vulnérabilité critique détectée. Aucun contenu S11+ n’a été ajouté.
