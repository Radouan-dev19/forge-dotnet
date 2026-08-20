# Décider d'échantillonner une trace

Implémentez `Submission.ShouldSample` avec la signature fournie.

Tracer chaque requête d'un service chargé coûte plus cher que le service lui-même. On n'en garde donc
qu'une fraction — sauf celles qui portent une erreur, car ce sont précisément celles qu'on relira.

## Le contrat

```csharp
public static bool ShouldSample(int traceHash, int percent, bool isError)
```

- `traceHash` est l'empreinte de l'identifiant de trace. Elle peut être **négative**.
- `percent` est le taux de conservation, un entier de **0 à 100 inclus**. Toute autre valeur lève
  `ArgumentOutOfRangeException`.
- `isError` indique que la trace porte une erreur.

## La règle

Une trace en erreur est **toujours** conservée, y compris à taux nul. C'est la seule exception, et
elle passe avant tout calcul.

Sinon, ramenez l'empreinte dans l'intervalle des pourcentages et conservez la trace lorsque cette
valeur est **strictement inférieure** au taux. Un taux de zéro ne conserve donc rien, un taux de cent
conserve tout.

La décision doit être **reproductible** : deux services qui voient la même trace prennent la même
décision. Aucun tirage au hasard.

## Avant d'écrire

Prédisez quatre cas : une empreinte négative, une empreinte exactement égale au taux, un taux nul
avec erreur, un taux nul sans erreur. Nommez ce qu'on perdrait en échantillonnant les erreurs comme
le reste.
