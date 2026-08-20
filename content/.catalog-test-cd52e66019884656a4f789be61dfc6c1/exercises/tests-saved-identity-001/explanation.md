# Explication

Un identifiant strictement positif prouve-t-il qu'une écriture a atteint la base ? Le prédicat
tient en une comparaison, et l'exercice porte sur ce que cette comparaison *observe* — la
question la plus fine du test d'intégration : qu'est-ce qui compte comme preuve de
persistance ?

Le mécanisme d'abord. Dans le motif classique, l'identifiant d'une entité neuve est attribué
*par la base* — colonne auto-incrémentée, séquence — au moment de l'insertion réelle. Avant
l'écriture, la propriété vaut zéro, la valeur par défaut du type ; après une insertion réussie,
la base a rendu un entier strictement positif que le contexte a recopié dans l'objet. Le
prédicat `id > 0` lit donc un *témoin* : la seule façon d'obtenir un identifiant positif est
que l'aller-retour ait eu lieu. Zéro signale l'absence d'attribution — l'insertion n'a pas eu
lieu, ou pas encore — et le négatif ne correspond à aucune attribution légitime : deux façons
de répondre faux, couvertes chacune par un cas.

La question de l'énoncé pointe le piège que ce témoin évite : que retournerait une relecture
depuis le contexte qui vient d'écrire ? L'objet lui-même — servi depuis la mémoire du
contexte, son cache de première main — *sans qu'aucune requête ne parte*. Un test qui écrit
puis relit via le même contexte peut être vert alors que rien n'a été validé en base :
l'écriture différée attend encore, la transaction n'est pas engagée, et le test valide un
aller-retour qui n'a jamais quitté le processus. C'est le faux positif le plus répandu des
tests de persistance. Les parades réelles se nomment : vérifier l'identifiant attribué — ce
prédicat —, relire par un *contexte neuf*, ou compter par une requête directe. L'exercice
isole la première, la plus légère ; les scénarios du laboratoire SQL pratiquent les autres.

La borne du prédicat est stricte et le cas caché la verrouille : un est le premier identifiant
légitime, zéro échoue — la frontière exacte entre « attribué » et « jamais écrit ».

Le coût est une comparaison ; la valeur est la doctrine. La transposition tient en une
question à poser devant chaque assertion d'un test d'intégration : *qui* me donne cette
information — la base, ou le cache de celui qui vient d'écrire ? Toute preuve qui ne traverse
pas la frontière réelle — le réseau, le disque, le processus d'en face — est une preuve de
mémoire, pas de persistance. Les tests qui distinguent les deux survivent aux changements de
configuration de contexte ; les autres découvrent la différence en production.
