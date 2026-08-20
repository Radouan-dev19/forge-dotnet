# Explication

La même expression que le repli optionnel du bloc langage — garde d'absence, rognage, valeur de
repli — mais l'angle est celui de la *qualité du code face au compilateur*, et l'énoncé le dit
par sa contrainte : sans opérateur de suppression. C'est un exercice sur le point d'exclamation,
et sur ce qu'il détruit.

Le contexte d'abord : les références nullables du langage moderne font du `null` une
information de *type*. Une `string?` peut être absente, une `string` ne le devrait pas, et le
compilateur suit les flux : après un test d'absence, il sait que la valeur est présente et
laisse déréférencer sans avertissement. Ce suivi transforme une classe entière d'erreurs
d'exécution — la référence nulle, la plus banale des exceptions de production — en
avertissements de compilation. Mais le système a une trappe : l'opérateur de suppression, le
point d'exclamation, qui dit au compilateur « fais-moi confiance, ce n'est pas nul ». L'énoncé
demande ce qu'il déplace : la vérification, de la *compilation* vers l'*exécution*. Chaque
suppression est une promesse non vérifiée — le compilateur se tait, et si la promesse est
fausse, l'exception revient, en production, là où le système de types l'avait éradiquée.

La version propre n'a rien à supprimer, et c'est le modèle : `IsNullOrWhiteSpace` *est* le test
que le compilateur comprend — après lui, dans la branche fausse, `value` est connu non nul, et
le `Trim()` se déréférence sans avertissement ni point d'exclamation. La garde ne sert pas
seulement la logique — absence et blanc vers le repli — elle sert le *raisonnement statique* :
elle donne au compilateur ce dont il a besoin pour prouver le reste. Écrire du code que
l'analyse comprend, plutôt que faire taire l'analyse, est toute la différence entre utiliser
les nullables et les subir.

La règle de revue qui en découle mérite d'être dite : chaque point d'exclamation dans un diff
est une question — « pourquoi le compilateur ne peut-il pas le voir ? ». Parfois la réponse
est légitime — une initialisation par framework, un contrat externe — et alors un commentaire
la documente. Le plus souvent, la réponse est qu'une garde manque ou qu'un type devrait être
nullable, et la suppression n'est que la dette qui masque le vrai correctif.

Les cas suivent l'énoncé : la valeur ordinaire rognée, les bordures blanches, le tout-blanc et
l'absent vers `n/a`. Le coût est négligeable.

La transposition : traiter les avertissements de nullabilité comme des erreurs — la
configuration du projet le permet —, réserver la suppression aux frontières documentées, et
lire chaque `!` en revue comme un signal, jamais comme une ponctuation.
