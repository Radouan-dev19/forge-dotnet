# Cartes de révision

- **Pourquoi le segment le plus long d'une trace est-il rarement le coupable ?** Parce qu'un segment
  englobe ses appels : les durées remontent vers la racine, et seul le temps propre — durée moins
  enfants directs — dit où le code travaille vraiment.
- **Pourquoi la soustraction du temps propre s'arrête-t-elle aux enfants directs ?** Parce que le
  temps des petits-enfants est déjà compté dans celui des enfants : soustraire la descendance
  entière compterait double et produirait des temps propres négatifs.
