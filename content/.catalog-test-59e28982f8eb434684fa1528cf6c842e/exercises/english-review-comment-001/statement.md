# Étiqueter un commentaire de revue

Implémentez `Submission.CommentKind` avec la signature fournie.

Vingt remarques sur une demande de fusion, toutes rédigées du même ton : l'auteur ne sait pas
lesquelles doivent être traitées avant de fusionner, et lesquelles sont des préférences. La
convention d'étiquetage règle ce problème en un mot placé en tête.

## Le contrat

```csharp
public static string CommentKind(string comment)
```

Trois étiquettes conventionnelles, chacune rendue sous sa catégorie :

| Étiquette écrite | Catégorie rendue | Ce qu'elle engage |
|---|---|---|
| `must` | `blocking` | à corriger avant de fusionner |
| `nit` | `suggestion` | une préférence, l'auteur décide |
| `q` | `question` | une demande d'explication, pas de correction |

L'étiquette est le texte qui précède le **premier deux-points**. La comparaison ignore la casse et
les blancs qui l'entourent.

## La règle

Tout le reste — étiquette absente, inconnue, ou commentaire sans deux-points — rend `unlabelled`.

Ce défaut est **délibérément le plus faible**. Traiter un commentaire non étiqueté comme bloquant
donnerait à toute remarque le pouvoir d'arrêter une fusion, y compris une préférence de style : la
revue se figerait sur des détails et cesserait d'être lue.

Une entrée absente lève `ArgumentNullException` : ne rien recevoir n'est pas la même chose que
recevoir un commentaire vide.

## Avant d'écrire

Prédisez quatre cas : une étiquette en majuscules, un deux-points au milieu d'une phrase sans
étiquette de tête, un commentaire vide, une étiquette inventée. Nommez ce qui se passerait si le
défaut était `blocking`.
