# Matrice du contenu C# initial — S1–S2

## Portée

Ce lot contient exactement dix exercices C# exécutables. Ils restent limités aux semaines 1 et 2 du parcours. Le chemin physique suit la racine du catalogue publié existant : `content/reference/exercises/`, soit la convention `exercises/{id}/` relativement à cette racine.

| Ordre | Semaine | Identifiant | Thèmes principaux | Difficulté | Méthode publique | Variante | Preuves automatisées |
|---:|---:|---|---|---:|---|---|---|
| 1 | S1 | `csharp-price-conversion-001` | types, `decimal`, conversion, arrondi | 1 | `ToCents(decimal)` | `csharp-shipping-decision-001` | nominal, zéro, demi-centime, négatif, valeurs variées |
| 2 | S1 | `csharp-shipping-decision-001` | conditions, bornes, montants | 1 | `ShippingCost(decimal, bool)` | `csharp-price-conversion-001` | seuil 80, express, négatif, valeur hors exemples |
| 3 | S1 | `csharp-loop-range-sum-001` | boucles, accumulateur, plage inclusive | 1 | `SumInclusive(int, int)` | `csharp-method-multiples-001` | nominal, négatifs, plage vide, borne unique, anti-constante |
| 4 | S1 | `csharp-method-multiples-001` | méthodes, paramètres, boucles | 2 | `CountMultiples(int, int, int)` | `csharp-loop-range-sum-001` | nominal, diviseur négatif, zéro interdit, plage vide |
| 5 | S2 | `csharp-array-differences-001` | tableaux, index, cas courts | 2 | `Differences(int[])` | `csharp-list-distinct-001` | nominal, deux éléments, vide, null, immutabilité |
| 6 | S2 | `csharp-list-distinct-001` | listes, unicité, ordre | 2 | `DistinctInOrder(List<int>)` | `csharp-array-differences-001` | doublons, négatifs, vide, null, immutabilité |
| 7 | S2 | `csharp-dictionary-stock-001` | dictionnaires, fusion, clés | 3 | `MergeStock(Dictionary<string,int>, Dictionary<string,int>)` | `csharp-string-frequency-001` | fusion, casse, vide, quantité négative, immutabilité |
| 8 | S2 | `csharp-string-frequency-001` | chaînes, normalisation, dictionnaires | 3 | `CountWords(string)` | `csharp-dictionary-stock-001` | casse, ponctuation, espaces, accents, null |
| 9 | S2 | `csharp-date-business-days-001` | dates, parcours, week-end | 3 | `CountBusinessDays(DateOnly, DateOnly)` | `csharp-date-expiry-001` | semaine, week-end, même jour, plage vide, changement de mois |
| 10 | S2 | `csharp-date-expiry-001` | dates, conditions, cas limites | 2 | `IsExpired(DateOnly, DateOnly, int)` | `csharp-date-business-days-001` | avant/à/après échéance, grâce, grâce négative |

## Contrat éditorial par exercice

Chaque dossier contient un manifeste v1, un énoncé autonome, un squelette compilable, trois cas visibles au minimum, des cas cachés de limites et d’anti-contournement, une solution compilable, une explication, quatre indices progressifs, des erreurs fréquentes, une variante appariée, deux cartes de révision et une question d’entretien dédiée.

Les tests cachés ne sont jamais ajoutés à une vue publique. Le runner les charge côté serveur, les chiffre avec une clé éphémère par tentative et ne transmet au processus soumis que les arguments du cas courant, jamais la valeur attendue ni les autres cas.

## Revue éditoriale manuelle

La revue du 28 juillet 2026 a résolu à la main un cas distinct par thème, puis confronté le raisonnement à la solution exécutée par le runner. Ces cas servent à relire l'énoncé ; ils ne remplacent ni les trois cas visibles ni les quatre cas cachés.

| Thème | Exercice relu | Cas résolu à la main | Conclusion |
|---|---|---|---|
| types et conversions | `csharp-price-conversion-001` | `1.235m` donne `124` centimes | ordre multiplication/arrondi/conversion explicite |
| conditions | `csharp-shipping-decision-001` | standard à `79m` donne `4.90m` | priorité express et seuil inclus non ambigus |
| boucles | `csharp-loop-range-sum-001` | `2..4` donne `9` | invariant et bornes incluses clairs |
| méthodes | `csharp-method-multiples-001` | `2..9`, diviseur `2`, donne `4` | contrat des trois paramètres complet |
| tableaux | `csharp-array-differences-001` | `[2, 5, 1]` donne `[3, -4]` | index, taille et immutabilité explicites |
| listes | `csharp-list-distinct-001` | `[2, 2, 1]` donne `[2, 1]` | ordre de première apparition préservé |
| dictionnaires | `csharp-dictionary-stock-001` | `Pen:1` fusionné avec `pen:2` donne `pen:3` | normalisation et validation préalable claires |
| chaînes | `csharp-string-frequency-001` | `Oui, oui; non` donne `oui:2, non:1` | séparateurs, casse et dernier mot définis |
| dates et parcours | `csharp-date-business-days-001` | mardi à jeudi inclus donne `3` | week-end et inclusion des bornes explicites |
| dates et cas limites | `csharp-date-expiry-001` | échéance J, aujourd'hui J+2, grâce `1` donne `true` | dernière date valide et comparaison stricte claires |

Pour chacun, H1 pose une question sans livrer la réponse, H2 localise le travail, H3 donne la stratégie et H4 fournit seulement un pseudo-code partiel. Les solutions expliquent le choix, la complexité et les erreurs fréquentes. Les variantes sont réciproques par paires ; les dix cibles ont été compilées et exécutées, ce qui couvre également l'exécution de chaque variante. Le protocole Practice de demande d'indice, de protection de la solution et de reprise par variante est couvert par les tests d'intégration et end-to-end, sans exposer de cas caché.

## Limites

Le lot n’introduit aucune notion de S3 ou ultérieure : pas de classe métier à concevoir, interface, héritage, exception personnalisée, LINQ obligatoire, fichier, JSON, asynchronisme, SQL, API, test framework, Git, Docker pédagogique ou cloud.

## Vérification reproductible

Depuis PowerShell, après construction de l'image `forge-dotnet-runner:test` :

```powershell
powershell -ExecutionPolicy Bypass -File scripts/verify-initial-csharp-content.ps1
```

Le script valide le catalogue, les dix identifiants, les variantes réciproques, les quatre niveaux d'indices, les fichiers éditoriaux, les trois cas visibles et quatre cas cachés de chaque exercice, puis exécute pour chacun la solution, le starter et une tentative codée en dur dans le runner Docker.
