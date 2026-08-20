# Explication

Compter les types d'événements distincts d'une liste : la chaîne se découpe, l'ensemble
déduplique, le cardinal répond. La solution tient en une expression, et son intérêt est ailleurs
— dans les deux mots de l'énoncé qui encadrent l'identité des événements : « définir la casse »
et « sans inventer de normalisation ».

Définir la casse d'abord. Le comparateur `Ordinal` est passé explicitement à l'ensemble : `A`
et `a` sont deux événements *différents*. Pour des codes d'événements — des identifiants
techniques produits par du code — c'est le choix juste, car la casse y est souvent
significative, et une fusion insensible masquerait une vraie anomalie : deux émetteurs qui
écrivent le même événement avec deux casses différentes est précisément le genre de divergence
qu'un dépouillement doit *révéler*, pas gommer. Le point de méthode dépasse le choix lui-même :
l'identité des éléments d'un ensemble est une clause de contrat, elle se déclare dans le
constructeur, et un ensemble construit sans comparateur explicite laisse le lecteur deviner.

Ne rien inventer ensuite, et c'est la leçon de débogage. La tentation devant des données
« sales » est de nettoyer en comptant : rogner les espaces, aplatir la casse, filtrer les
inconnus. Chaque nettoyage non demandé change le compte — et un compte d'événements qui ne
correspond pas au journal brut est un instrument de mesure faussé. Un outil de diagnostic
rapporte ce qui est ; les segments ` A` et `A` restent distincts si le producteur les a écrits
ainsi, car cette distinction *est* une information — probablement le bug d'un émetteur. La
seule tolérance du contrat est écrite : les segments vides s'ignorent — `RemoveEmptyEntries` —
pour absorber les virgules doublées et terminales, un artefact de format et non une donnée.

Les bornes suivent la convention de comptage : l'entrée absente, vide ou blanche rend zéro —
rien à dépouiller. Une chaîne sans virgule est un seul événement ; une chaîne de virgules
seules n'en contient aucun, tous les segments étant vides. Les cas cachés jouent ces bords et
mêlent les casses pour figer l'identité choisie.

Le coût est linéaire, allocations de découpage comprises — un dépouillement de journal réel
éviterait de matérialiser les segments, mais l'échelle de l'exercice ne le justifie pas.

La transposition est une paire de questions à poser devant tout comptage de catégories issues
de texte : qu'est-ce qui rend deux valeurs identiques — casse, espaces, préfixes ? — et chaque
normalisation appliquée est-elle une exigence du contrat ou une invention du code ? La première
se répond par un comparateur ; la seconde, toujours, par « demandée ou retirée ».
