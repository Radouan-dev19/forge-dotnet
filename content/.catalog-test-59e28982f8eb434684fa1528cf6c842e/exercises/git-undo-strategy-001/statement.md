# Choisir comment annuler un commit

Implémentez `Submission.UndoStrategy` avec la signature fournie.

Trois façons d'annuler, et le choix ne dépend pas de votre confort mais de deux faits : les commits
ont-ils été **publiés**, et voulez-vous **garder le travail** dans votre copie ?

## Le contrat

```csharp
public static string UndoStrategy(int commitsBack, bool alreadyPushed, bool keepChanges)
```

| Situation | Stratégie rendue |
|---|---|
| les commits sont publiés | `revert` |
| sinon, le travail doit rester dans la copie | `reset-soft` |
| sinon | `reset-hard` |

`commitsBack` est le nombre de commits à défaire, en remontant depuis le sommet. Il doit être
**strictement positif** : annuler zéro commit n'est pas une demande, c'est une erreur d'appel, et la
fonction lève `ArgumentOutOfRangeException`.

## Pourquoi cet ordre

La publication passe en premier parce qu'elle **interdit** les deux autres. Un retrait, doux ou dur,
réécrit l'histoire : il déplace le sommet de la branche et abandonne des commits. Tant qu'ils
n'existent que chez vous, personne ne s'en aperçoit. S'ils sont publiés, ils vivent aussi ailleurs,
et votre réécriture ne les efface pas — elle crée une divergence que le prochain qui synchronise
devra démêler.

L'annulation par commit inverse est la seule opération qui **ajoute** au lieu de retirer : elle
fabrique un commit qui défait les modifications, et laisse le commit d'origine dans l'historique.
C'est parfois vu comme un défaut — l'erreur reste visible — c'est en réalité la propriété qui la rend
sûre en public.

Entre les deux retraits, la différence est ce qu'il advient de votre travail : le retrait doux
ramène les modifications dans la copie, prêtes à être recommittées ; le retrait dur les jette.

## Avant d'écrire

Prédisez quatre cas : un commit publié qu'il faut défaire, deux commits locaux à recommitter
autrement, un commit local à jeter, une demande portant sur zéro commit. Nommez ce que le retrait dur
détruit sans possibilité de retour.
