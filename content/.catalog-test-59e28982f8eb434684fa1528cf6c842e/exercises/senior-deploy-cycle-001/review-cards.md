# Cartes de révision

- **Pourquoi un cycle d'appels vaut-il verdict de monolithe distribué ?** Parce qu'il abolit l'ordre
  de livraison : chaque service soudé a besoin de l'autre pour livrer, tester et redémarrer — tous
  les coûts du réseau, aucun bénéfice du découpage.
- **Un service qui appelle un cycle sans en faire partie est-il en cycle ?** Non : aucun chemin ne
  revient à lui, il garde sa liberté de livraison — le classer noierait le signal sous tout le
  bassin versant du cycle.
