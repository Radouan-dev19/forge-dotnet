# Explication

L’état de requête impose scoped ; un service partagé doit être explicitement sans état. La solution sépare validation et décision, sans état externe. Complexité : **O(1) en temps et O(1) en espace**. Les cas cachés varient les frontières et réfutent une constante mémorisée. Après lecture, la tentative n’est pas maîtrisée : expliquez la règle avec vos mots et planifiez une reprise à blanc.
