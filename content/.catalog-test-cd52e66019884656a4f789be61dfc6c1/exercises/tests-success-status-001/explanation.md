# Explication

« La requête a-t-elle réussi ? » se code en une plage : la famille des succès va de deux cents
à deux cent quatre-vingt-dix-neuf inclus. L'exercice est court parce que sa leçon est ciblée :
tester une *famille*, pas un représentant.

L'énoncé demande ce qu'un test limité au statut nominal laisserait passer, et la liste des
implémentations fautives qui acceptent deux cents est édifiante : l'égalité stricte
`== 200` — qui déclarerait échouées les créations à deux cent un et les réponses sans contenu à
deux cent quatre —, la plage décalée `< 299` — qui exclut le dernier de la famille —, la plage
trop large `<= 300` — qui absorbe le premier des redirections. Toutes passent le test « deux
cents réussit » ; aucune ne survit aux quatre frontières — deux cents et deux cent
quatre-vingt-dix-neuf qui répondent vrai, cent quatre-vingt-dix-neuf et trois cents qui
répondent faux. C'est la mécanique désormais récurrente des valeurs limites, appliquée à une
plage que tout développeur d'API manipule quotidiennement — et qu'une proportion étonnante de
code de production teste par `== 200`, jusqu'au jour où un serveur légitime répond deux cent
deux et casse l'intégration.

La sémantique derrière la plage mérite sa phrase : la famille des deux cents signifie « le
serveur a compris et accepté » — les nuances internes — créé, accepté, sans contenu — sont des
informations *supplémentaires*, pas des degrés de succès. Un client générique traite la famille
uniformément, puis raffine s'il a besoin de la nuance. C'est ce que le prédicat encode, et
c'est pour cela qu'il prend la plage entière plutôt qu'une liste de statuts choisis : les codes
de succès rares — ou futurs — appartiennent d'office à la famille.

La forme `is >= 200 and <= 299` reprend le motif de plage lisible : les deux bornes visibles
au même endroit, dans l'ordre de lecture, chacune modifiable indépendamment — et donc chacune
verrouillable par son couple de cas de frontière. Le domaine hors famille n'a pas besoin
d'être validé : un statut négatif ou fantaisiste répond simplement faux, le prédicat classant
sans juger la vraisemblance de l'entrée.

Le coût est constant. La transposition est le réflexe des plages standardisées : familles de
statuts, plages de ports, classes d'adresses, intervalles de codes d'erreur — chaque fois
qu'un protocole définit une famille par plage, le code la teste par plage, et le plan de test
pose ses quatre frontières. Le test du seul représentant célèbre — deux cents, quatre-vingts,
quatre cent quatre — est la version illusoire de la couverture, et cet exercice existe pour
qu'on la reconnaisse au premier coup d'œil en revue.
