# Explication

Préserver l'ordre d'entrée et inclure la borne.

La borne est inclusive : un élément exactement égal au minimum est conservé. C'est la seule frontière du problème, et c'est là que se joue la différence entre une implémentation correcte et une implémentation qui passe les cas nominaux.

L'ordre est un contrat au même titre que le contenu. Rien n'oblige un filtre à préserver l'ordre en général ; ici le contrat l'annonce, donc trier le résultat serait une régression même si l'ensemble des valeurs retenues est identique. Le parcours est linéaire et l'espace correspond au résultat produit.
