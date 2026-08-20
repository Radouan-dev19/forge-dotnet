# Cartes de révision

- **Quels deux axes suffisent à choisir un double de test ?** Le sens du flux — la dépendance
  fournit des données ou reçoit des effets — et la nature du contrat : réponse toute faite,
  comportement, état relisible ou protocole d'appels.
- **Pourquoi refuser d'espionner une dépendance de données entrantes ?** Parce que le test
  cimenterait le nombre de lectures, un détail que tout remaniement honnête peut changer : c'est la
  sur-spécification qui fait casser les suites pour rien.
