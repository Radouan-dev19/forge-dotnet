# Explication

Valider total et taux puis arrondir une seule fois le net.

Ce qui est retourné est le net, et la formule le dit directement : le total multiplié par ce qui reste après remise. Passer par une remise intermédiaire oblige à l'arrondir avant de la soustraire, donc à arrondir deux fois le même calcul — un écart d'un centime apparaît alors sur certaines valeurs.

Le taux est borné des deux côtés. Au-delà de un, le complément devient négatif et le net aussi, ce qui n'a aucun sens métier et se propagerait sans erreur visible. Le mode d'arrondi, enfin, se déclare : celui de la plateforme est statistiquement correct et surprenant sur une facture, où l'attente est un arrondi qui s'éloigne de zéro. La décision est en temps constant.
