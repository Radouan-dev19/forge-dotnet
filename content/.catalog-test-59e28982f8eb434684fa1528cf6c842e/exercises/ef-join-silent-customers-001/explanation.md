# Explication

Cet exercice a l'air de porter sur un seuil ; il porte sur une absence. « Les clients qui n'ont aucune
commande atteignant le seuil » est une question en creux, et les questions en creux sont celles que
les jointures internes trahissent le plus naturellement — le défaut ne se voit pas dans le code, ne
lève aucune erreur, et retire précisément les lignes les plus intéressantes du résultat.

**Pourquoi Margaret disparaît d'une jointure interne.** Une jointure interne ne produit une ligne que
lorsque les deux côtés existent. Le client sans commande n'a rien à apparier : il n'entre jamais dans
le produit de la jointure, donc jamais dans le filtre, donc jamais dans le résultat — quelle que soit
la condition écrite ensuite. Or, pour une campagne de relance, le client qui n'a jamais commandé est
la cible par excellence. C'est le schéma classique du bogue de périmètre : la requête est juste pour
les lignes qu'elle voit, et fausse par ce qu'elle ne voit pas. La parade tient en une règle : quand la
question est une absence, on part de l'ensemble qui existe toujours — ici les clients — et on exprime
l'absence comme une négation d'existence sur l'autre ensemble.

**Ce que la propriété de navigation traduit réellement.** Écrire la négation d'un `Any` sur la
collection de commandes n'est pas un confort d'écriture : le fournisseur la traduit en sous-requête
d'inexistence côté serveur, évaluée pendant le parcours des clients. Le moteur ne matérialise jamais
la jointure complète ; il vérifie, client par client, qu'aucune ligne qualifiante n'existe. C'est la
forme que prendrait la même question en SQL manuscrit, et la voir émerger d'une expression objet est
exactement la compétence que l'accès aux données par un traducteur de requêtes demande : savoir ce que
le code devient, pas seulement ce qu'il dit.

**Pourquoi le filtrage en mémoire est refusé même quand il rend la même chaîne.** Sur quatre clients,
tout fonctionne ; c'est ce qui rend l'erreur durable. Charger la table puis filtrer en mémoire déplace
le travail du serveur — indexé, proche des données — vers l'application, et le coût croît avec la
table, pas avec le résultat. Le jour où la base compte cent mille clients, la requête en creux rend
toujours quelques centaines de noms, tandis que le chargement intégral transfère tout, à chaque appel.
La différence entre les deux écritures est invisible dans le résultat et décisive dans le plan.

**Le tri appartient à la requête.** Trier les noms après coup fonctionne aussi — et disperse la
sémantique : la requête rendrait un ordre non spécifié que le code compenserait. Confier l'ordre au
serveur documente le contrat au seul endroit qui l'exécute, et prépare la pagination, qui exige un
ordre stable côté serveur.

**Le refus du seuil non positif ferme la porte à la question vide.** Avec un seuil nul, toute commande
qualifie et la cible se réduit aux clients sans commande — une autre question, qui mérite sa propre
requête, pas un cas dégénéré de celle-ci.
