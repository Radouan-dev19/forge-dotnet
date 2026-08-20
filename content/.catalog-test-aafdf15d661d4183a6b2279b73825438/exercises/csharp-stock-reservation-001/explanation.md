# Explication

Peut-on réserver ? Une comparaison répond — et pourtant l'exercice contient deux frontières et
un choix de régime d'erreur qui méritent chacun leur phrase, car ils décident de commandes
réelles.

La frontière principale est l'égalité : réserver *exactement* le stock disponible est accepté.
« Accepter exactement la quantité disponible », dit le contrat, et `requested <= stock`
l'encode — le dernier article se vend. La version écrite `<` refuserait cette vente : un
magasin qui garde toujours un exemplaire invendable, défaut invisible tant que les tests évitent
l'égalité, et que le cas caché posé pile dessus expose. Vider un stock à zéro est aussi un cas
licite qui en découle : demander zéro sur un stock nul est cohérent — rien demandé, rien à
refuser.

Le régime des entrées invalides est le choix le plus discutable de la solution, et il faut
savoir le défendre *et* le contester. Un stock ou une demande négatifs ne décrivent rien ; la
fonction répond `false` — refus — plutôt que de lever. C'est un parti pris de fonction de
*décision* : elle répond à « la réservation peut-elle se faire ? », et une demande absurde ne le
peut pas, quelle que soit la raison. L'alternative par exception se défendrait dans une couche
de validation, où le négatif révèle un bug amont à faire remonter. La différence entre les deux
régimes — verdict contre validation — traverse tout le catalogue, et cet exercice est du côté
verdict : les données étranges donnent un refus calme, jamais un incident. L'important est que
les cas cachés fixent ce choix : stock négatif refuse, demande négative refuse, et personne ne
peut « corriger » le régime sans casser un test.

Un dernier mot sur ce que la fonction *ne fait pas* : elle ne réserve rien. Elle est pure — deux
entiers entrent, un verdict sort — et cette pureté est ce qui la rend testable par table. Dans
un système réel, le verdict et l'action seraient séparés par une transaction : vérifier puis
réserver sans protection laisse deux clients passer la même vérification et survendre le
dernier article. La fonction pure est la *règle* ; sa mise en œuvre concurrente est un autre
problème, traité par la base de données ou un verrou — savoir situer cette limite est
exactement ce qu'un entretien sonde derrière ce genre de question simple.

Le coût est constant. La transposition : plafonds de crédit, quotas d'appels, capacités de
salle — chaque « peut-on consommer X sur Y ? » repose les mêmes questions. L'égalité passe-t-elle ?
Que font les négatifs ? Et la décision est-elle séparée de l'action qui consomme ?
