# Explication

Additionner les deux valeurs décimales sans conversion binaire.

Le type décimal représente exactement les valeurs de la base dix, ce qu'un type à virgule flottante binaire ne fait pas : un centime y devient une valeur voisine, et l'écart s'accumule à chaque opération. Sur des montants, cette différence se lit directement sur une facture.

Le contrat n'annonce aucun arrondi : en ajouter un modifierait le résultat sans que personne ne l'ait décidé, et masquerait l'écart que l'appelant voulait peut-être observer. L'addition est en temps constant.
