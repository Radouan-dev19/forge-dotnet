# Explication

Retirer seulement les espaces de bordure sans modifier le contenu interne.

La restriction est le sujet de l'exercice : le retrait porte sur les bordures, pas sur les espaces internes. Une saisie contenant deux mots doit rester deux mots. Étendre l'opération à tous les blancs paraît plus propre et change le contrat sans le dire.

L'entrée absente et la chaîne de blancs ne reçoivent pas le même traitement ici. La première est une faute d'appelant et lève ; la seconde est une saisie valide qui se réduit à une chaîne vide. D'autres exercices de ce parcours prennent la décision inverse — c'est le contrat qui tranche, jamais l'habitude. Le coût est linéaire dans la longueur de la saisie.
