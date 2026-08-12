# Explication

Distinguer absence conventionnelle, plage invalide et valeur acceptée.

Trois états, et non deux : une valeur manquante et une valeur hors plage n'appellent pas la même correction chez l'appelant. Les fondre en un seul refus produit un message que le client ne peut pas exploiter — c'est précisément ce qu'un corps d'erreur normalisé cherche à éviter en nommant le champ et la raison.

L'ordre des tests rend l'état d'absence atteignable : la valeur conventionnelle appartient aussi à la plage refusée, donc tester la plage en premier l'absorberait. Les deux bornes de la plage se testent séparément, dans les deux sens. La décision est en temps constant.
