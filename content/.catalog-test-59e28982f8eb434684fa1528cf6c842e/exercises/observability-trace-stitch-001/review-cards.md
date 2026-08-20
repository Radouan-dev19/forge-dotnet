# Cartes de révision

- **Pourquoi juger les parents d'une trace en deux passages ?** Parce que le collecteur reçoit dans
  l'ordre d'arrivée réseau : un enfant précède souvent son parent, et le juger à la volée fabrique de
  faux orphelins au rythme des latences.
- **Qu'indique un taux d'orphelins qui monte dans un journal de traces ?** Une casse de propagation
  du contexte à un endroit précis — file, intergiciel, bibliothèque — visible dans la masse alors que
  chaque exemplaire semble anecdotique.
