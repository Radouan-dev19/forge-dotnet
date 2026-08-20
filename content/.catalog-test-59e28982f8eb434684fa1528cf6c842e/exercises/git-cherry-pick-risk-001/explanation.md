# Explication

Reporter un commit d'une branche vers une autre rejoue une modification dans un contexte qui n'est
plus celui où elle a été écrite. La question de l'exercice n'est pas « cela va-t-il passer » mais
« si cela passe, est-ce toujours la même intention ».

**Le partage de fichiers prime sur l'écart, et c'est le point à retenir.** L'intuition classe au
nombre de commits de divergence : deux cents commits d'écart semblent forcément plus risqués que
trois. C'est faux, et le contre-exemple est immédiat — deux cents commits qui ne touchent aucun des
fichiers modifiés par le commit reporté ne créent aucune interférence, tandis que trois commits sur
la même fonction peuvent avoir déplacé exactement les lignes que le report cherche à modifier. Une
implémentation qui classe au seul écart se trompe précisément là où l'on avait besoin d'elle.

L'écart n'est donc pas inutile : il mesure la **probabilité** que le contexte autour des lignes ait
changé. Mais il ne mesure quelque chose que sur les fichiers que le commit touche réellement. C'est
pourquoi il est testé **à l'intérieur** de la branche des fichiers communs, jamais avant.

**Le refus d'un commit de fusion n'est pas une prudence excessive.** Un commit de fusion a deux
parents ; ses modifications ne sont définies que par rapport à l'un d'eux. Reporter « ce commit »
sans dire lequel n'a pas de réponse unique : selon le parent choisi, l'ensemble des lignes reportées
diffère complètement. Un outil qui accepte quand même applique une convention silencieuse, et
produit un contenu que personne n'a demandé. Le refus oblige l'appelant à trancher, au moment où il
sait encore ce qu'il voulait.

**Le seuil est exclusif, et cela se justifie.** À l'écart seuil exactement, la dérive n'est pas
encore réputée élevée. Choisir la borne large ferait basculer un cas de plus dans la catégorie la
plus alarmante, sans qu'aucun argument ne le distingue du précédent. Le point important n'est pas la
valeur du seuil — cinquante est une convention d'équipe — mais le fait qu'il soit **nommé** et
identique pour tous, plutôt que réévalué à vue par chaque personne.

**Ce qu'un risque faible ne garantit pas**, et qu'il faut savoir dire : l'absence de conflit textuel
n'est pas l'absence de problème. Un commit reporté peut s'appliquer proprement et dépendre d'une
fonction qui n'existe pas encore sur la branche cible, ou d'un comportement qui y a changé. Le
conflit est un signal syntaxique ; la cohérence sémantique reste à vérifier par les tests, et c'est
la raison pour laquelle un report n'est jamais complet sans exécuter la suite de la branche cible.

Le coût est constant : une garde et deux tests.
