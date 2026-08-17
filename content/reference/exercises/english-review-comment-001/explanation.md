# Explication

La fonction tient en six lignes ; la décision qu'elle encode est celle qui rend une revue de code
lisible dans une équipe internationale.

Sans étiquetage, vingt remarques rédigées du même ton laissent l'auteur deviner lesquelles bloquent
la fusion. Il devine mal, et les deux issues sont mauvaises : soit il traite tout, et une préférence
de nommage retarde une correction de sécurité ; soit il ne traite rien, et la remarque qui comptait
se perd. Un mot en tête suffit à lever l'ambiguïté, et c'est la raison d'être de la convention.

**Le défaut est le point de conception.** Un commentaire non étiqueté rend la catégorie la plus
faible, jamais la plus forte. Prendre `blocking` comme valeur par défaut donnerait à n'importe quelle
phrase le pouvoir d'arrêter une fusion — y compris un « looks good to me ». La revue se figerait sur
des détails, et l'équipe finirait par la contourner. Quand un défaut doit être choisi, on prend celui
dont l'erreur coûte le moins.

**L'étiquette se lit en tête, et nulle part ailleurs.** Chercher `must` n'importe où dans le texte
classerait « I must admit this is clever » comme bloquant. Le premier deux-points est la frontière
convenue ; ce qui le précède est l'étiquette, ce qui le suit est le propos. Un deux-points en tête
ne laisse rien à lire et retombe donc sur le défaut.

**La normalisation est asymétrique, et c'est voulu.** L'étiquette est comparée sans casse parce que
la convention est un usage, pas une syntaxe : personne ne doit voir sa remarque déclassée pour avoir
écrit `MUST`. La catégorie rendue, elle, sort toujours sous une forme unique, parce qu'elle sera
comparée par du code.

La table des conventions est isolée pour une raison pratique : reconnaître une quatrième étiquette —
`praise`, par exemple, que certaines équipes ajoutent pour rendre visible ce qui est bien fait — ne
demande qu'une ligne, sans toucher à la logique de lecture.

**La normalisation est asymétrique, et c'est voulu.** L'étiquette est comparée sans casse parce que
la convention est un usage, pas une syntaxe : personne ne doit voir sa remarque déclassée pour avoir
écrit en capitales. La catégorie rendue, elle, sort toujours sous une forme unique, parce qu'elle
sera comparée par du code — un tableau de bord, un filtre, une règle de fusion automatique.

**Pourquoi cette convention relève de l'anglais professionnel.** Dans une revue entre personnes qui
ne partagent pas leur langue maternelle, le ton se lit mal. Une remarque écrite poliment par un
anglophone peut sembler sèche à qui la lit vite ; une remarque écrite platement par un non-anglophone
peut sembler agressive alors qu'elle ne l'est pas. L'étiquette retire cette charge : elle dit ce que
la remarque engage, indépendamment de la manière dont elle est formulée. C'est un cas où la
convention protège l'auteur autant que le lecteur — celui qui écrit `nit:` n'a plus besoin de
chercher les formules d'atténuation qui lui manquent peut-être.

**Ce que l'exercice n'essaie pas de faire.** Il ne juge pas la qualité d'une remarque, ne détecte pas
un ton agressif, ne devine pas l'intention. Toutes ces choses demandent un lecteur humain, et un
programme qui prétendrait les évaluer produirait un verdict que rien ne pourrait vérifier. La
frontière est nette : la machine classe ce que l'auteur a déclaré, l'humain juge le reste. C'est la
même frontière qui court dans tout ce parcours, entre ce qu'une suite de tests peut prouver et ce
qu'elle ne prouvera jamais.
