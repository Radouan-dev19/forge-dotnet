# Explication

Un utilisateur détient une liste de rôles séparés par des virgules ; détient-il celui qui est
requis ? La réponse naïve — `roles.Contains(required)` — tient en une ligne et accorde des
droits qu'elle ne devrait pas : c'est elle que l'exercice fait réfuter, et l'énoncé demande de
nommer le droit qu'une recherche partielle accorde à tort.

Le mécanisme de la faille : `Contains` cherche une *sous-chaîne*, pas un élément. Un
utilisateur dont la liste porte `SuperOperator` — un rôle qui contient le mot `Operator` —
passe le test `Contains("Operator")` sans détenir ce rôle ; un `Reader` passe le test
`Contains("Read")`. Chaque paire de rôles dont l'un est préfixe ou fragment de l'autre devient
une élévation de privilèges silencieuse, et les nomenclatures réelles regorgent de ces paires.
La correction structurelle est de comparer des *segments complets* : découper la liste sur son
séparateur, puis tester l'égalité — entière — de chaque segment avec le rôle requis. Le
passage de « chercher dans la chaîne » à « comparer les éléments de la liste » est tout
l'exercice : une donnée qui *encode* une liste doit être décodée avant d'être interrogée.

Les détails d'hygiène complètent la robustesse. Les options de découpage combinent le rognage
des segments et l'élimination des vides : une liste saisie avec des espaces — `Reader, Operator`
— ou des virgules doublées se compare proprement, sans que les blancs ne fassent échouer une
égalité légitime. Le rôle requis est rogné symétriquement. La comparaison ignore la casse —
choix de contrat pour des noms de rôles, qui sont des identifiants d'administration saisis par
des humains, contrairement aux identifiants machine des exercices voisins ; la nuance entre
les deux régimes est une décision à savoir argumenter. Et l'absence — liste vide, rôle requis
blanc — répond faux : le refus est le défaut des contrôles d'accès.

Les cas suivent l'énoncé : le rôle détenu qui passe, le rôle contenu-dans-un-autre qui ne
passe *pas* — le verrou anti-sous-chaîne —, les segments espacés qui passent, la liste vide
refusée.

Le coût est linéaire dans la taille de la liste, allocations de découpage comprises — sans
enjeu à l'échelle de quelques rôles.

La transposition est double. Côté sécurité : tout contrôle d'appartenance — rôles, portées,
groupes, permissions — se fait sur des éléments décodés, jamais par recherche de sous-chaîne
dans la représentation. Côté général : chaque fois qu'une chaîne transporte une structure —
liste, paire, chemin —, la première opération est le décodage, et les opérations suivantes
travaillent sur la structure. Interroger l'encodage est le raccourci qui finit en faille.
