# Évaluer le risque d'un report de commit

Implémentez `Submission.CherryPickRisk` avec la signature fournie.

Reporter un commit d'une branche vers une autre rejoue ses modifications dans un contexte qui n'est
plus celui où elles ont été écrites. Le risque n'est pas de « ne pas compiler » : c'est que le
report réussisse et produise autre chose que l'intention d'origine.

## Le contrat

```csharp
public static string CherryPickRisk(int commitsBetween, bool touchesSameFiles, bool isMergeCommit)
```

| Situation | Risque rendu |
|---|---|
| le commit est une fusion | `refuse` |
| sinon, aucun fichier commun | `low` |
| sinon, écart supérieur à cinquante commits | `high` |
| sinon | `medium` |

`commitsBetween` est le nombre de commits qui séparent les deux branches depuis leur ancêtre commun.
Un écart négatif lève `ArgumentOutOfRangeException`.

## Pourquoi cet ordre

Le **refus** passe en premier. Un commit de fusion a deux parents : reporter ses modifications
suppose de dire par rapport auquel des deux on les mesure. Sans cette précision, l'opération n'a pas
de sens unique — et une commande qui accepte quand même produit un contenu que personne n'a voulu.

Ensuite vient le **partage de fichiers**, et c'est le point de l'exercice. L'écart entre branches
mesure une dérive de contexte, mais une dérive dans des fichiers que le commit ne touche pas ne le
concerne pas. Deux cents commits d'écart sans un seul fichier commun sont moins risqués que trois
commits d'écart sur le même fichier. Beaucoup d'implémentations classent au seul écart et se
trompent exactement dans ce cas.

L'écart n'intervient donc qu'**en présence de fichiers communs**, où il mesure la probabilité que le
contexte autour des lignes reportées ait changé.

## Avant d'écrire

Prédisez quatre cas : une fusion, un écart énorme sans fichier commun, un écart faible avec fichier
commun, un écart au-delà du seuil avec fichier commun. Nommez ce qu'un report réussi sans conflit
peut quand même casser.
