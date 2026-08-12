# Explication

Appliquer seulement les transitions autorisées et ignorer les événements hors état.

Une machine à états n'est pas une suite d'affectations : chaque transition est conditionnée par l'état de départ, et c'est cette condition qui rend le comportement reproductible. Appliquer une transition sans la vérifier revient à accepter n'importe quel ordre d'événements, ce qui produit des états impossibles et un diagnostic sans fin.

Ignorer un événement inattendu plutôt que de lever est une décision de robustesse : un flux d'événements réel contient des répétitions et des retards, et la machine doit rester utilisable. Le rejeu part toujours de l'état initial, sans mémoire entre deux appels, ce qui rend chaque exécution reproductible. Le parcours est linéaire et un seul état est conservé.
