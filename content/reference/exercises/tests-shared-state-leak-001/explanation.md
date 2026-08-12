# Explication

« Il passe chez moi, il échoue sur la machine d'intégration. » Derrière cette phrase se cache presque
toujours le même mécanisme : un test s'appuie sur une donnée qu'un autre a laissée derrière lui. Il est
vert, il ne prouve rien, et il devient rouge le jour où l'ordre d'exécution change — parce qu'on a
ajouté un test, parce qu'on a filtré la suite, parce que le lanceur a décidé de paralléliser.

**Deux états cohabitent, et ils n'ont pas la même durée de vie.** C'est le point qui fait la
difficulté du sujet, et la raison pour laquelle une seule collection ne suffit pas. La base traverse
toute la suite : elle n'est jamais réinitialisée entre deux tests, sinon il n'y aurait pas de problème
à détecter. Ce que le test courant a inséré, lui, ne dure que son passage. Une clé présente dans la
base peut donc avoir deux origines radicalement différentes, et une seule collection les rend
indiscernables — c'est exactement l'erreur qui fait échouer les premières tentatives.

**Une lecture sur clé absente n'est pas une fuite, et il faut résister à l'envie de la signaler.**
Elle produit un test rouge, immédiatement, à l'endroit du problème. Un test rouge est une bonne
nouvelle : il dit la vérité. La fuite est le cas inverse et bien plus coûteux — un test vert qui ne
vérifie rien de ce qu'il prétend vérifier. Confondre les deux ferait du détecteur un outil qui accuse
les tests honnêtes.

**Réinsérer une clé déjà présente rend le test autonome.** La nuance surprend et elle est juste : peu
importe que la clé traînait déjà, ce test l'a mise lui-même, donc il passera seul. Ce n'est pas lui qui
dépend de l'ordre. Le suivant, en revanche, hérite maintenant de deux sources possibles et n'a rien
inséré : c'est celui-là qu'il faut nommer. Cette règle empêche d'accuser un test correct simplement
parce qu'il vit dans une suite sale.

**Ne rapporter que le premier test fuyant est un choix de diagnostic.** Un défaut de nettoyage
contamine en général tous les tests qui suivent, et les énumérer produirait une liste de symptômes où
la cause se perd. Le premier nom pointe vers la zone où le nettoyage manque, ce qui est l'information
actionnable.

**Ce que le détecteur ne dit pas.** Il ne dit pas qui a oublié le nettoyage, seulement qui en dépend.
La correction ne consiste d'ailleurs presque jamais à ajouter des suppressions une par une : elle
consiste à donner à chaque test son propre jeu de données, ou à envelopper chacun dans une transaction
annulée à la fin. Ce détecteur sert à mesurer le problème et à prouver qu'il a disparu, pas à le
corriger.

Le coût est linéaire dans le nombre d'opérations, avec un espace proportionnel au nombre de clés
distinctes. Les deux ensembles se remplacent avantageusement par des tables de hachage, ce qui rend
chaque test d'appartenance constant.
