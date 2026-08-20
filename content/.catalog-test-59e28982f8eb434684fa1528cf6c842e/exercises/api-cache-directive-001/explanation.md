# Explication

Composer une directive de cache est un classement à quatre issues où l'ordre des tests n'est pas
un détail de style : c'est une décision de sécurité. Une donnée sensible marquée publique par
erreur est la pire issue du domaine — un cache partagé la servira à des inconnus —, et c'est
pourquoi la sensibilité se teste *en premier*, avant que toute autre nature ne puisse la
contredire.

La sensibilité rend `no-store` : le contenu ne doit laisser aucune trace stockée, ni disque, ni
mémoire, ni cache intermédiaire, parce que sa persistance même est le risque. Aucune durée de
fraîcheur n'apparaît alors — insérer un `max-age` dans une directive qui interdit le stockage
serait contradictoire, et c'est une erreur qu'un test caché débusque. La nature personnelle rend
`private` : seul le cache du client final — son navigateur — peut garder la réponse, jamais un
cache partagé. La confusion `public`/`private` est la faille classique : marquer publique une
réponse propre à un utilisateur autorise un intermédiaire à la resservir au suivant, et ce bug ne
se reproduit pas en développement, où il n'y a ni cache partagé ni second utilisateur. La nature
publique, enfin, rend `public` : la réponse est la même pour tous, un catalogue, une page d'aide.

Le défaut mérite d'être conscient : une nature *inconnue* retombe sur `no-store`, pas sur
`public`. C'est la présomption de prudence, cousine de la présomption de danger du versionnage :
face à une réponse qu'on ne sait pas classer, on refuse de la mettre en cache plutôt que de
l'exposer. Le sens du défaut est le vrai enjeu — un défaut permissif exposerait par mégarde ce
qu'on n'a pas su nommer.

La `max-age` n'apparaît que là où le cache est permis — les branches personnelle et publique — et
sa valeur est validée : une durée négative n'a pas de sens, et la refuser à l'entrée évite de
composer une directive absurde. La normalisation de la nature — rognage, casse invariante —
traite l'étiquette comme l'identifiant technique qu'elle est.

Le coût est constant. La transposition est le principe du classement par priorité de risque :
quand plusieurs catégories peuvent s'appliquer à une même donnée, on teste d'abord celle dont
l'erreur coûte le plus cher — ici l'exposition d'une donnée sensible —, et on adopte un défaut
prudent pour l'inconnu. Cette discipline se retrouve dans le classement des remarques de revue,
le tri des risques de diff, la sévérité des alertes : la catégorie la plus grave se décide en
premier, et le défaut protège.
