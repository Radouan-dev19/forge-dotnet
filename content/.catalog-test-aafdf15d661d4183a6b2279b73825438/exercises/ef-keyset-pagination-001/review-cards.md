# Cartes de révision

- **Qu'est-ce qui rend la pagination par jeu de clés stable sous écritures concurrentes ?** La page
  se définit par son contenu — les lignes après un identifiant — et non par une position que chaque
  insertion en amont décale.
- **Pourquoi la comparaison au curseur est-elle stricte ?** Parce que le curseur est le dernier
  identifiant déjà lu : une comparaison large le servirait à nouveau à chaque frontière de page, un
  doublon discret et systématique.
