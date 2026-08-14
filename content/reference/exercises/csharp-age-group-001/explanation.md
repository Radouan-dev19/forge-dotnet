# Explication

Un classement par tranches paraît être l'exercice le plus simple du catalogue ; c'est en fait un
exercice de spécification, et les quatre `return` successifs de la solution sont la transcription
d'un tableau de décision qu'il faut savoir écrire avant le code.

La structure en gardes ordonnées — invalide, puis mineur, puis adulte, puis senior — a une
propriété qui fait toute sa valeur : chaque condition s'appuie sur l'échec des précédentes. Quand
la ligne `age < 18` s'exécute, on sait déjà que l'âge est positif ou nul ; quand `age < 65`
s'exécute, on sait qu'il vaut au moins dix-huit. Les intervalles sont donc exprimés sans bornes
doubles ni `&&`, et il est impossible qu'un âge tombe dans deux catégories ou dans aucune. La
version alternative à conditions indépendantes — quatre `if` complets avec bornes basses et
hautes — dit la même chose en deux fois plus de texte et offre deux fois plus d'occasions de se
tromper sur une borne. L'ordre des gardes n'est pas un détail d'écriture : il *est* la
spécification.

Le cas invalide illustre une décision de contrat qui revient sans cesse : que faire d'une entrée
hors domaine ? Ici, le choix est de rendre un état nommé — la chaîne `invalid` — plutôt que de
lever. C'est défendable pour un classement destiné à l'affichage ou à la statistique, où l'on
veut compter les entrées aberrantes sans interrompre le lot ; l'exception serait préférable dans
un parcours d'inscription où l'âge négatif révèle un bug amont. L'exercice impose la première
convention et, surtout, impose qu'elle soit *distincte* : fondre les âges négatifs dans les
mineurs fabriquerait des statistiques fausses en silence.

Les cas cachés se concentrent là où ce genre de code meurt : sur les frontières exactes.
Dix-sept et dix-huit doivent tomber de part et d'autre ; soixante-quatre et soixante-cinq aussi ;
moins un doit être invalide et zéro doit être mineur. Un seul caractère — `<` devenu `<=` —
déplace une frontière d'une unité, et seul un cas posé précisément sur elle le voit. Écrire ces
cas *avant* le code, comme l'énoncé le demande, est la vraie compétence entraînée.

Le coût est constant et sans intérêt ; la transposition, elle, est partout. Tranches de tarifs,
seuils d'alerte, barèmes d'imposition, niveaux de gravité : tout classement par intervalles
contigus se code par gardes ordonnées, se spécifie par ses frontières, et se teste par un cas
posé sur chaque borne plus un cas hors domaine. L'exercice tient en dix lignes parce que cette
recette-là tient en trois phrases — et qu'elle suffit.
