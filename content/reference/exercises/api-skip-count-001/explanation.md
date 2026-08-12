# Explication

Valider page et taille avant un calcul vérifié du décalage.

La numérotation commence à un, donc le décalage se calcule sur le numéro diminué de un. Omettre cette soustraction saute la première page entière : le défaut est invisible sur la page deux, qui affiche bien des résultats, et ne se remarque qu'en comparant des totaux.

Les deux bornes protègent le reste. Un numéro de page invalide produirait un décalage négatif, refusé plus loin par la base avec un message obscur. Une taille non bornée permettrait un décalage arbitrairement grand, et la multiplication vérifiée signale le dépassement plutôt que de produire une valeur négative silencieuse. La décision est en temps constant.
