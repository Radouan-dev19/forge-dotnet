# Cartes de révision

- **Combien d'attentes contient une campagne de n tentatives avec recul ?** Exactement n moins une :
  la première tentative part sans délai et personne n'attend après la dernière.
- **Comment doubler une attente sans jamais déborder ?** En comparant au plafond avant de doubler :
  la valeur courante reste sous le plafond, donc son double reste calculable, et l'écrêtage borne
  toute la suite.
