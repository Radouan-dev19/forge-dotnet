# Normaliser un nom de branche

Implémentez `Submission.NormalizeBranchName` avec la signature fournie.

Deux personnes qui décrivent le même travail écrivent rarement le même nom de branche :
`Feature/Import CSV`, `feature/import-csv`, `Feature/Import_CSV`. Sur un système de fichiers
sensible à la casse, ces trois-là sont **trois branches différentes**. La normalisation ramène une
intention à une seule écriture.

## Le contrat

```csharp
public static string NormalizeBranchName(string raw)
```

Survivent à la normalisation : les **lettres minuscules**, les **chiffres**, le **tiret** et la
**barre oblique** — celle-ci sépare les espaces de noms, `feature/`, `fix/`.

Tout autre caractère — espace, souligné, accent, ponctuation — devient un **tiret**. Le convertir
plutôt que le supprimer est délibéré : supprimer collerait deux mots en un seul, et
`import csv` deviendrait `importcsv`.

Les majuscules passent en minuscules.

Une suite de tirets se réduit à **un seul**, et les tirets de bordure sont retirés.

## Les refus

Une entrée absente lève `ArgumentNullException`. Un nom qui ne laisse **rien** après normalisation —
que des séparateurs, que des blancs — lève `ArgumentException` : une branche sans nom n'est pas une
branche.

## Avant d'écrire

Prédisez quatre cas : un nom avec espaces et majuscules, un nom déjà normalisé, un nom entouré de
séparateurs, un nom qui ne contient aucun caractère utilisable. Nommez ce qui arrive à une équipe
quand deux branches ne diffèrent que par une majuscule.
