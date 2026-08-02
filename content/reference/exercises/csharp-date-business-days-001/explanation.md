# Explication

`DateOnly` exprime qu’aucune heure ni zone ne participe au calcul. La boucle examine chaque date incluse ; elle ignore explicitement `DayOfWeek.Saturday` et `DayOfWeek.Sunday`, puis avance d’un jour.

Les tests de bornes unitaires rendent l’inclusion observable. Le coût dépend du nombre de jours de la plage, sans allocation proportionnelle.
