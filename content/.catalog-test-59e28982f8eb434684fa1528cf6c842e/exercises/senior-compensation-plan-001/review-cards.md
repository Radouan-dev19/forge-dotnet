# Cartes de révision

- **Pourquoi une saga compense-t-elle dans l'ordre inverse de l'exécution ?** Parce que les étapes
  tardives reposent sur les précoces : l'ordre inverse garde à chaque instant un état-préfixe
  interprétable — décisif quand la compensation échoue elle-même.
- **Pourquoi les gestes de compensation se cataloguent-ils au lieu de se déduire des noms ?** Parce
  que compenser est une décision métier — un correctif pour une notification, parfois un avoir pour
  un débit — et qu'inventer le geste d'une étape inconnue exécuterait une action que personne n'a
  validée.
