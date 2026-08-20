# Explication

Un filtre de recherche par terme : l'utilisateur tape trois lettres, l'API renvoie les lignes
qui les contiennent. Derrière ce geste banal, deux décisions font la différence entre un filtre
prévisible et un filtre qui rend des résultats différents selon le serveur qui répond.

La première est le comparateur, et l'énoncé en fait son titre : *sans dépendre de la culture*.
`Contains` avec `OrdinalIgnoreCase` compare binairement, en aplatissant seulement la casse —
deux serveurs configurés dans deux cultures rendent le même verdict, un test écrit sur un poste
passe sur tous. L'alternative culturelle a des surprises documentées : selon la culture, des
séquences se contractent ou s'étendent, des caractères s'ignorent, et le même appel trouve ici
ce qu'il ne trouve pas là. Pour un filtre technique — celui d'une API, d'une grille, d'un
journal — l'ordinal est le seul choix défendable ; la comparaison culturelle se réserve aux
fonctionnalités linguistiques assumées, tri alphabétique d'une interface par exemple. La
casse, elle, est aplanie par confort d'usage : personne ne s'attend à ce que « ada » rate
« Ada », et le cas caché de casse croisée le fige.

La seconde est le refus du terme vide, et l'énoncé demande de nommer ce qu'un vide accepté
retournerait : *tout*. Une chaîne vide est contenue dans n'importe quelle chaîne — c'est la
définition — et le filtre deviendrait l'absence de filtre : la grille entière, le journal
entier, renvoyés à un appelant qui croyait avoir cherché quelque chose. Sur une table de
production, c'est une réponse de plusieurs mégaoctets et une base sollicitée pour rien. Le
refus par `false` — plutôt qu'une exception — est le choix du verdict calme : un terme blanc
est une saisie ratée, pas une attaque, et « aucune correspondance » est la réponse qui ne casse
rien. La valeur absente ou blanche suit la même convention, par symétrie.

Le rognage du terme — pas de la valeur — complète l'ergonomie : les espaces de bordure d'une
zone de recherche sont du bruit de saisie, l'intérieur de la valeur cherchée est de la donnée.

Les cas cachés jouent les trois axes de l'énoncé : correspondance exacte, casse différente,
terme vide ou blanc refusé — plus l'absence de correspondance franche.

Le coût est linéaire dans la taille de la valeur. La transposition est le trio de tout filtre
textuel d'API : comparateur ordinal nommé, casse décidée explicitement, entrées dégénérées
refusées avant d'atteindre les données. Trois lignes de contrat qui évitent le ticket classique
— « la recherche ne trouve pas la même chose en recette et en production » — dont la cause est
presque toujours un comparateur laissé au défaut.
