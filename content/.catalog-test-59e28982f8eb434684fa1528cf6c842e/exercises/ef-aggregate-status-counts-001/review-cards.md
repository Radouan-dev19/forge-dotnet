# Cartes de révision

- **Où se pose un filtre qui porte sur le compte d'un groupe ?** Après le regroupement — c'est
  l'équivalent du having relationnel : posé avant, le même prédicat filtre des lignes et répond à une
  autre question.
- **Que transfère une agrégation bien placée côté serveur ?** Les groupes survivants seulement : le
  volume traité croît avec la table, le volume transféré avec le nombre de groupes retenus — le
  regroupement en mémoire inverse ce rapport.
