# Calculer la fenêtre de rotation d'un rafraîchissement

Implémentez `Submission.NextTokenLifetime(int absoluteExpiryUnix, int nowUnix,
int slidingLifetimeSeconds)`.

À chaque rotation, le guichet émet un nouveau jeton de rafraîchissement. Sa durée de vie est
bornée par deux horloges : la durée de glissement — la fenêtre normale entre deux rotations — et
l'échéance absolue de la session, qu'aucune rotation ne peut repousser. La méthode calcule la
durée, en secondes, du prochain jeton.

Règles exactes :

- une durée de glissement nulle ou négative est une faute d'appel : `ArgumentOutOfRangeException` ;
- si l'instant courant atteint ou dépasse l'échéance absolue, la session est finie : rendez `0` —
  c'est un état ordinaire, pas une erreur ;
- sinon, rendez le plus petit entre la durée de glissement et le reste de session — l'écart
  entre l'échéance absolue et l'instant courant ;
- effectuez la soustraction d'instants en 64 bits avant de comparer.

Les instants des tests sont fixes et factices. Écrivez avant le code : la valeur rendue à
l'instant exact de l'échéance, une seconde avant, et le moment précis où la fenêtre cesse de
valoir la durée de glissement pour valoir le reste.

Exemple : entrée `[1750010000, 1749990000, 3600]`, sortie `3600`.
