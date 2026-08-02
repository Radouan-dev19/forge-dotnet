# Explication

Le contrat définit une borne incluse : la date d’échéance augmentée du délai de grâce est encore valide. La comparaison correcte est donc strictement `today > lastValidDate`.

Valider la grâce avant `AddDays` empêche une valeur incohérente de déplacer la limite dans le passé. Le calcul et la comparaison sont constants.
