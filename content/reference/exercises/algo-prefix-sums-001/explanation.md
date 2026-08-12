# Explication

Chaque case contient la somme du préfixe se terminant à cet index.

La définition fixe l'ordre des deux opérations : on ajoute d'abord, on écrit ensuite. L'inverse produit les sommes des préfixes se terminant avant l'index — une convention également utile, mais différente, et qu'il faudrait alors annoncer.

L'intérêt de cette table n'apparaît qu'à l'usage : la somme de n'importe quel segment se lit ensuite par une seule soustraction, en temps constant, quelle que soit sa longueur. C'est un investissement linéaire qui rend gratuites toutes les interrogations suivantes. Le contrôle de dépassement traite la longue suite de valeurs positives, et l'espace correspond au résultat.
