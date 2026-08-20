# Classer un constat de revue par gravite

Implementez `Submission.Triage` avec la signature fournie. La fonction recoit l'identifiant d'un
constat de revue de code et rend son classement sous la forme `severite:categorie`.

## Les valeurs

La gravite est `blocking` ou `minor`. La categorie est `correctness`, `security`, `concurrency` ou
`style`. Le classement des constats connus est le suivant :

- `missing-null-check` et `off-by-one` : `blocking:correctness` ;
- `sql-injection` et `hardcoded-secret` : `blocking:security` ;
- `unsynchronized-shared-state` : `blocking:concurrency` ;
- `variable-naming` et `missing-doc-comment` : `minor:style`.

## La regle

Trois familles bloquent une fusion parce qu'elles menacent le comportement, la surete ou la
correction sous concurrence : `correctness`, `security`, `concurrency`. Le **style** ne bloque
jamais : une preference de nommage est un avis, pas un veto. Presenter un constat de style comme
bloquant est un **faux positif** qui coute : il brouille le signal et use la confiance dans la revue.

Un identifiant hors de la table connue rend `unknown`. Un identifiant nul leve
`ArgumentNullException`.

## Avant d'ecrire

Predisez le classement d'une injection, d'une faute de nommage et d'un identifiant inconnu. Nommez ce
que coute, concretement, une revue qui bloque sur du style.
