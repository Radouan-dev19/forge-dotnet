# Cartes de révision

- **D'où viennent les doublons qu'une fenêtre de déduplication laisse passer ?** Des reprises —
  segment rejoué, partition réconciliée, restauration — qui relivrent des heures plus tard, quand la
  fenêtre a oublié : le régime normal, lui, est attrapé.
- **Une livraison qui échappe à la fenêtre rafraîchit-elle la mémoire du magasin ?** Oui : le
  magasin enregistre ce qu'il traite sans savoir que c'était un doublon — et ce doublon réappliqué
  protège paradoxalement les suivants.
