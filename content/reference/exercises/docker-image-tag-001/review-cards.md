# Cartes de révision

- **L'étiquette implicite.** Une référence sans deux-points porte la mouvante : `app` et
  `app:latest` désignent la même chose, et c'est le cas que le contrôle naïf laisse passer.
- **Le dernier deux-points.** Un registre privé s'écrit avec un port ; chercher le premier
  séparateur lirait le port comme une étiquette et laisserait tout passer en silence.
