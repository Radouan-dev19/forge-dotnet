# Cartes de révision

- **Pourquoi une jointure interne trahit-elle les questions en creux ?** Parce qu'elle ne produit de
  ligne que lorsque les deux côtés existent : le client sans commande n'entre jamais dans le filtre,
  et c'est justement lui la cible.
- **Que devient la négation d'un Any sur une propriété de navigation ?** Une sous-requête
  d'inexistence évaluée côté serveur pendant le parcours des clients — la forme exacte qu'aurait la
  question en SQL manuscrit.
