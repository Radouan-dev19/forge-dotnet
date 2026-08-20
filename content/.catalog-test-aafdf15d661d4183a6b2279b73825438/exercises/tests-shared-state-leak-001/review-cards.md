# Cartes de révision

- **Pourquoi deux ensembles et non un seul ?** Parce qu'une clé présente dans la base peut venir du
  test courant ou d'un test antérieur, et qu'une seule collection rend ces deux origines
  indiscernables.
- **Pourquoi une lecture sur clé absente n'est-elle pas une fuite ?** Parce qu'elle rend le test rouge
  à l'endroit du problème. La fuite est l'inverse : un test vert qui ne vérifie rien.
