# Cartes de révision

- **Pourquoi un percentile au rang rend-il une valeur réellement observée ?** Parce qu'il désigne
  une mesure du jeu trié, jamais une interpolation : le p99 est une requête qu'on peut retrouver
  dans les traces, pas une moyenne de deux voisines que personne n'a subie.
- **Que garantit le plafond dans le calcul du rang d'un percentile ?** La promesse de couverture —
  au moins la part demandée des mesures est sous la valeur rendue — que l'arrondi au plus proche
  casse sur les petits échantillons des fenêtres d'alerte.
