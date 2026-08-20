# Cartes de révision

- **À quoi reconnaît-on un test dépendant de l'ordre dans un journal multi-exécutions ?** Au fait
  qu'il a reçu au moins deux verdicts différents pour le même code : la place occupée dans la suite
  est la seule chose qui a changé.
- **Pourquoi refuser de comparer deux exécutions qui ne couvrent pas les mêmes tests ?** Parce qu'un
  test absent n'a pas de verdict : le compter comme divergent fabrique de faux instables, le compter
  comme stable masque les vrais.
