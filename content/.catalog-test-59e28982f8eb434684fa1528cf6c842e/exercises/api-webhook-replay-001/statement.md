# Fermer la fenêtre de rejeu d'un webhook

Implémentez `Submission.IsWithinReplayWindow(int timestamp, int nowUnix, int toleranceSeconds)`.

La signature d'un webhook prouve son origine, mais pas sa fraîcheur : un envoi authentique
capturé peut être rejoué tel quel. La parade est de vérifier son horodatage — signé, donc
infalsifiable — contre une fenêtre étroite autour de l'instant courant. La méthode répond si
l'envoi tombe dans cette fenêtre.

Règles exactes :

- `toleranceSeconds` négatif est une faute d'appel : `ArgumentOutOfRangeException` ;
- la fenêtre est *symétrique* : l'envoi est accepté si l'écart absolu entre `nowUnix` et
  `timestamp` ne dépasse pas `toleranceSeconds` — un envoi légèrement en avance, dû à une horloge
  d'émetteur rapide, est aussi légitime qu'un envoi légèrement en retard ;
- la borne est inclusive : à un écart exactement égal à la tolérance, l'envoi passe ;
- calculez l'écart en 64 bits, les horodatages pouvant être éloignés.

Une tolérance nulle est valide : elle n'accepte que l'instant exact. Écrivez avant le code : un
envoi frais, un envoi à la borne exacte, un envoi trop vieux d'une seconde, et un envoi en avance.

Exemple : entrée `[1749990000, 1749990030, 300]`, sortie `true`.
