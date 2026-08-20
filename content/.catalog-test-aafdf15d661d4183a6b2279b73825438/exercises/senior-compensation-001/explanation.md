# Explication

Une saga remplace la transaction que l'on n'a plus des lors qu'une operation traverse plusieurs
services. Chaque etape valide immediatement son effet local : la reservation est ecrite, le paiement
est capture, l'expedition est ordonnee. Aucun verrou global ne tient l'ensemble, donc quand une etape
tardive echoue, on ne peut pas simplement faire un rollback : les effets deja produits existent
vraiment ailleurs. La compensation est la reponse a ce probleme : pour chaque etape reussie, une
action qui en defait l'effet metier.

L'ordre est le coeur de l'exercice, et il est contre-intuitif au premier regard. On compense dans
l'ordre inverse de l'application parce que les etapes tardives sont souvent construites sur les
premieres. Annuler l'expedition avant le paiement, puis le paiement avant la reservation, respecte
les dependances : on demonte dans le sens oppose a celui ou l'on a monte. Compenser dans l'ordre
direct reintroduirait exactement le genre d'incoherence que la saga cherche a effacer, en liberant
une ressource dont une compensation ulterieure a encore besoin.

Le noyau decidable reduit ce raisonnement a une transformation pure : lire les etapes reussies dans
l'ordre recu, puis les parcourir du dernier indice vers le premier en prefixant chaque nom par
`undo-`. Ce prefixe materialise la distinction entre une etape et son annulation ; les confondre
reviendrait a rejouer l'action au lieu de la defaire.

Les cas caches deplacent les bornes que l'implementation naive rate. Une saga d'une seule etape doit
rendre exactement une compensation, sans separateur superflu. Une saga vide, ou une saga dont aucune
etape n'a reussi, rend une chaine vide : il n'y a rien a defaire, et fabriquer une action serait un
effet de bord invente. L'etape qui a echoue ne figure jamais dans la liste, car elle n'a produit
aucun effet a compenser ; l'inclure declencherait une annulation d'une operation qui n'a pas eu lieu.

Le cout d'une erreur ici est eleve et silencieux. Une compensation dans le mauvais ordre laisse le
systeme dans un etat partiellement defait qu'aucun test unitaire local ne detecte, parce que chaque
service pris isolement est coherent ; seule la vue d'ensemble ne l'est pas. C'est pourquoi
l'ordonnancement de la compensation se raisonne a froid, sur papier, avant d'ecrire la moindre ligne.

La transposition depasse la saga : toute sequence d'effets reversibles, d'une migration par lots a un
deploiement multi-etapes, se defait dans l'ordre inverse de sa pose. Retenir cette regle, c'est
disposer d'un reflexe qui vaut bien au-dela du seul patron saga.
