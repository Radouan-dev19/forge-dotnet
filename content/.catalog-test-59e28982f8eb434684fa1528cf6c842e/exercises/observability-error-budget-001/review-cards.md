# Cartes de révision

- **Pourquoi le budget d'erreur alloué s'arrondit-il vers le bas ?** Parce que tout autre arrondi
  offre, sur les petites fenêtres, un échec entier que l'objectif ne concède pas : le plancher est le
  seul arrondi qui ne dépasse jamais la promesse.
- **Pourquoi rendre un budget restant négatif au lieu de l'écrêter à zéro ?** Parce que le
  dépassement chiffré est le signal : moins deux se rattrape en gelant un déploiement, moins deux
  cents déclenche une revue d'incident.
