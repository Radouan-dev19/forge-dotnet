# Explication

Lire un dictionnaire paraît ne rien demander ; en réalité, C# offre trois manières de le faire,
et cet exercice existe pour qu'on choisisse la bonne en connaissance de cause.

L'indexeur `stock[key]` lève `KeyNotFoundException` quand la clé manque. C'est le bon outil
quand l'absence est *anormale* — une clé qu'on vient d'insérer, un invariant interne. Ici,
l'absence est un cas métier ordinaire : demander le stock d'une référence jamais approvisionnée
est une question légitime dont la réponse est zéro, pas un incident. Transformer chaque
interrogation en exception potentielle obligerait tous les appelants à des blocs d'interception,
et une exception par lecture sur un chemin fréquent a en plus un coût mesurable.

`ContainsKey` suivi de l'indexeur répond au problème mais interroge la table *deux fois* — deux
calculs de hachage, deux recherches — pour une information que la première interrogation
possédait déjà. `TryGetValue` fait le travail en une seule interrogation : il rend un booléen de
présence et dépose la valeur dans une variable de sortie. L'expression conditionnelle qui suit
traduit le contrat mot à mot : la valeur si elle existe, zéro sinon. C'est la forme idiomatique,
et la connaître évite l'aller-retour le plus recopié des revues de code débutantes.

Les deux gardes d'entrée départagent des situations que l'exercice tient à séparer. Un
dictionnaire `null` n'est pas un stock vide : c'est l'appelant qui n'a rien fourni, et le
signaler par `ArgumentNullException` fait remonter le bug à sa source. Une clé nulle, vide ou
blanche, en revanche, ne désigne aucune référence : la convention du contrat est zéro, cohérente
avec « référence inconnue ». On pourrait défendre l'exception ici aussi ; l'important est que la
frontière soit écrite et testée — les cas cachés interrogent la clé absente, la clé blanche et
le dictionnaire vide, et vérifient qu'aucune de ces lectures ne *modifie* la table, car le
contrat promet une lecture pure. C'est un piège réel : certains utilitaires « lire ou créer »
insèrent la valeur par défaut en passant, et une simple consultation se met à écrire.

Le coût est celui d'une interrogation de table de hachage — constant en moyenne — et c'est
exactement pour cela qu'on paie un dictionnaire.

La transposition dépasse le stock : configuration avec valeur par défaut, compteur qu'on lit
avant de l'avoir incrémenté, cache dont l'absence signifie « pas encore calculé ». Chaque fois,
la même question ouvre l'analyse — l'absence est-elle un cas normal ou une anomalie ? — et la
réponse choisit entre `TryGetValue` et l'indexeur. Le réflexe une-seule-interrogation, lui, vaut
dans tous les cas.
