# Cartes de révision

- **Quel critère interdit un découpage avant toute motivation organisationnelle ?** Les invariants
  partagés — données écrites en commun ou transaction commune : les découper fabrique une
  transaction distribuée pour résoudre un problème d'organisation.
- **Pourquoi la lecture seule de données partagées n'interdit-elle pas le découpage ?** Parce
  qu'elle se sert par réplication ou par cache sans invariant commun : seule une fraîcheur se
  négocie, là où l'écriture partagée verrouille.
