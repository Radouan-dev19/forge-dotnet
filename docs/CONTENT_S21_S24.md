# Contenu S21–S24 — Azure optionnel, exploitation et preuves finales

## Périmètre livré

Lʼincrément 10, validé le 6 août 2026, matérialise les semaines 21 à 24 sans modifier lʼarchitecture de Forge.NET. Azure reste un sujet pédagogique et un exemple dʼinfrastructure inspectable : aucun compte, abonnement, service distant ou paiement nʼest requis pour charger le catalogue, construire les starters ou réussir les preuves locales. La commande de référence complète, les contrôles dédiés et le parcours manuel simulé sont verts.

| Semaine | Leçons | Exercices automatisables | Preuve principale |
|---:|---:|---:|---|
| 21 | 3 | 2 | choix App Service/Container Apps, séparation SQL/Storage et identité gérée/Key Vault |
| 22 | 3 | 2 | signaux corrélés, alerte/coût borné et incident simulé résolu |
| 23 | 2 | 1 | architecture du projet final et jalon révisable avec rollback |
| 24 | 2 | 1 | défense en anglais, preuve de carrière et bref dʼincident responsable |
| **Lot 10** | **10** | **6** | mode local reproductible, sans création Azure |

Chaque exercice possède deux tests visibles, deux tests cachés différents, quatre indices progressifs, une solution protégée, une explication, des erreurs fréquentes, une variante réelle, deux cartes de révision et une question dʼentretien liée. Le runner exécute seulement les fonctions C# déterministes ; il ne contacte jamais Azure.

## Matrice finale des volumes

| Famille | Avant 10 | Ajout 10 | Total final | Exigence |
|---|---:|---:|---:|---:|
| Leçons | 60 | 10 | **70** | 70 minimum |
| Exercices C#/algo cumulés | 129 | 6 | **135** | 80 minimum |
| dont API/tests/sécurité | 35 | 0 | **35** | 35 minimum |
| dont Git/Docker/CI/Azure | 9 | 6 | **15** | 15 minimum |
| DebugLabs | 25 | 0 | **25** | 25 minimum |
| Scénarios SQL/EF | 40 | 0 | **40** | 40 minimum |
| Questions dʼentretien | 128 | 62 | **190** | 190 exact |
| Cartes dʼanglais S21–S24 | 0 | 50 | **50** | 50 exact |
| Activité dʼanglais historique hors lot de cartes | 1 | 0 | **1** | conservée, non comptée comme carte |
| Mini-projets | 8 | 0 | **8** | 8 exact |
| Projet final guidé | 0 | 1 | **1** | 1 exact |
| Examens | 6 | 2 | **8** | 8 exact |
| Documents du catalogue `content/reference/` | 352 | 129 | **481** | schémas et références valides |
| Fichiers du catalogue `content/reference/` | 1 812 | 202 | **2 014** | aucun placeholder |

Les 190 questions se répartissent en **120 junior, 50 intermédiaire et 20 avancé**. Le lot 10 ajoute 14 junior, 28 intermédiaire et 20 avancé : six questions sont directement liées aux nouveaux exercices et 56 couvrent des décisions distinctes dʼhébergement, données, identité, coût, observabilité, incident, performance, projet, carrière et défense en anglais.

Les 50 cartes dʼanglais forment 25 paires complémentaires : une production écrite et une réponse orale par situation. La répartition est 16 cartes B1, 20 B2 et 14 C1. Chaque carte impose décision, preuve, limite et suite ; les situations écrites et orales restent distinctes. `reference-glossary-001` demeure une activité historique et nʼest pas reclassée artificiellement dans ce volume.

## Azure, coût et mode simulé

`content/labs/azure-operations/` contient :

- un plan Bicep montrant App Service ou Container Apps, Azure SQL, Storage, Key Vault, Managed Identity, Log Analytics et Application Insights ;
- un starter .NET 10 sans paquet externe ni appel réseau ;
- une télémétrie factice et un script dʼincident déterministe ;
- un contrôle local qui inspecte les garde-fous, construit puis exécute le starter et résout lʼincident.

Le plan nʼembarque ni identifiant Azure, ni donnée personnelle, ni valeur sensible. Les identifiants opérateur et la référence dʼimage sont des paramètres sans valeur commitée. Storage refuse lʼaccès anonyme et les clés partagées ; Azure SQL refuse le réseau public et prévoit une administration Entra ; les deux choix dʼhébergement utilisent une identité système. Le dépôt ne lance aucune commande de création Azure.

Un déploiement réel, sʼil est décidé hors Forge.NET, reste manuel, facultatif et potentiellement facturé. La procédure exige groupe dédié, taille minimale vérifiée au moment de lʼessai, budget et alerte, propriétaire, heure de suppression, suppression explicite puis contrôle quʼaucune ressource facturable ne subsiste. Une alerte de budget nʼest jamais décrite comme un arrêt automatique.

## Projet final et examens

`project-final-service-operations-001` fournit cinq jalons et une grille à six critères totalisant exactement 100 %. Le brief impose monolithe modulaire, parcours critique, persistance reproductible, tests, sécurité, incident simulé et défense. Il ne contient ni squelette métier, ni modèle final, ni code de remise, ni solution complète. Le mode Azure simulé satisfait entièrement le jalon dʼexploitation.

- Examen 7 : `azure-observability-v1`, 15 candidats, tirage de 8, 120 minutes, seuil 80 %.
- Examen 8 : `final-readiness-v1`, 16 candidats répartis de S1 à S24, tirage de 8, 150 minutes, seuil 80 %.

Les banques ne contiennent que leur manifeste et des identifiants dʼexercices existants. Solutions, réponses dʼentretien et tests cachés restent dans les sources privées existantes. Lʼexamen 8 est la partie technique automatisée ; la défense du projet reste une preuve manuelle annoncée comme telle.

## Carrière et confidentialité

`content/reference/career/` fournit matrice de preuves CV, carnet STAR, suivi minimal de candidatures, préparation à la négociation, trame des trente premiers jours et export Markdown. Ces outils utilisent un exemple entièrement fictif, refusent une coordonnée directe détectable et avertissent que CV et suivi peuvent contenir des données personnelles.

Le kit ne promet ni emploi, ni entretien, ni salaire. Il demande de nommer un laboratoire comme projet personnel, de distinguer contribution et résultat collectif, de ne jamais inventer une métrique et de retirer données et métadonnées inutiles avant partage. La trame post-embauche est seulement un plan adaptable ; aucun parcours post-embauche nʼest implémenté.

## Vérifications reproductibles

L'échafaudeur ci-dessous ne réécrit aucun fichier existant : il rapporte les documents conservés et
n'écrit que les manquants. Il ne régénère un lot déjà publié qu'avec `-Force`, ce qui détruit toute
reprise éditoriale.

```powershell
powershell -ExecutionPolicy Bypass -File scripts/New-S21S24Content.ps1
dotnet build --no-restore
dotnet test --no-build --filter "Category=ContentS21S24"
powershell -ExecutionPolicy Bypass -File content/labs/azure-operations/Verify-LocalMode.ps1
powershell -ExecutionPolicy Bypass -File scripts/verify.ps1
rg -n "secret|password|token|connection string" content docs
```

La recherche large produit nécessairement des occurrences pédagogiques et des règles de refus. Chaque correspondance doit être relue ; aucune valeur ne doit être masquée ou supposée sûre parce quʼelle apparaît dans une documentation. Les tests dédiés recherchent en plus les formes caractéristiques de clés privées, chaînes de compte, signatures partagées et jetons réels.

## Limites assumées

- Le Bicep est inspecté comme support pédagogique ; aucune validation réelle par Azure nʼest revendiquée.
- Les coûts et API Azure évoluent ; toute répétition réelle doit revoir disponibilité, prix et versions le jour de lʼessai.
- Le p95 du laboratoire est une valeur calculée sur quatre observations factices, pas une mesure de production.
- Les réponses modèles dʼanglais et dʼentretien servent à la revue après tentative ; leur consultation ne prouve pas la maîtrise.
- Le projet final reste entièrement à produire et défendre par lʼapprenant.
