# Explication

La compensation est ce qui reste de la transaction quand la transaction n'est plus possible. À
l'intérieur d'une base, l'annulation est gratuite : rien n'est visible avant la validation. À travers
plusieurs services, chaque étape est **déjà visible** au moment où la suivante échoue — le stock est
réservé, la carte est débitée — et il n'existe aucun bouton retour. La saga assume cette réalité :
elle découpe l'opération en étapes locales, et prépare pour chacune le geste qui la défait. L'exercice
porte sur la partie que les présentations du motif escamotent : l'ordre et la discipline de ces
gestes.

**Pourquoi l'ordre inverse est structurel.** Les étapes tardives reposent sur les précoces : le
transporteur est réservé pour un stock réservé, le débit correspond à une commande créée. Compenser
dans l'ordre d'exécution défait la fondation d'abord — la commande est annulée pendant que le débit
et la réservation existent encore, et tout observateur — un service, un rapport, un client — voit un
paiement sans commande. L'ordre inverse garantit une propriété plus forte qu'il n'y paraît : à chaque
instant de la compensation, l'état global est un **préfixe** de la saga — comme si elle s'était
simplement arrêtée plus tôt. Et cette propriété paie précisément quand la compensation échoue à son
tour : l'état où elle s'interrompt est interprétable, reprennable, au lieu d'être un gruyère.

**Pourquoi les compensateurs se cataloguent au lieu de se deviner.** La tentation est de dériver le
geste inverse du nom de l'étape — préfixer « un- », inverser le verbe. Mais la compensation n'est pas
une symétrie de nommage, c'est une décision métier : compenser une notification n'est pas « ne pas
notifier » — c'est envoyer un correctif ; compenser un débit n'est pas toujours un remboursement —
c'est parfois un avoir. Le catalogue rend chaque décision explicite et relue ; l'étape hors catalogue
se refuse parce que lui inventer un geste, c'est exécuter une action métier que personne n'a validée.

**Pourquoi l'étape répétée se refuse.** Un journal qui montre deux fois le même débit ne décrit pas
une saga : les étapes d'une saga sont idempotentes ou uniques par conception. Produire deux
remboursements « pour être sûr » est exactement le genre de sur-correction qui transforme un incident
technique en incident comptable. Le refus renvoie au vrai problème — le journal, pas le plan.

**Ce que le journal reçu dit en creux.** L'étape qui a échoué n'y figure pas : elle n'a rien produit
à défaire. Cette hypothèse — chaque étape est atomique, elle a entièrement eu lieu ou pas du tout —
est la charge de chaque service participant, et c'est elle que les revues de conception de saga
doivent vérifier en premier.

En entretien, le motif se nomme saga et ses gestes compensating actions ; la question qui suit est
presque toujours celle de l'énoncé — « et si la compensation échoue ? » — et la réponse tient à
l'ordre inverse : on reprend là où elle s'est arrêtée, sur un état qui a encore un sens.
