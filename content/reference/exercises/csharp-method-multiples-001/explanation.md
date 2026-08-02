# Explication

Une méthode claire sépare son contrat de son mécanisme. `divisor == 0` est refusé avant la boucle. Pour tout diviseur non nul, `value % divisor == 0` exprime directement la divisibilité, y compris avec des valeurs négatives.

Le compteur ne change que lorsque le prédicat est vrai. La plage inversée n’effectue aucune itération. Le temps dépend du nombre de valeurs parcourues ; l’espace reste constant.
