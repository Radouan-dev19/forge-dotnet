# Explication

Vérifier la signature d'un webhook, c'est répondre à « cet envoi vient-il vraiment de l'émetteur
attendu, et n'a-t-il pas été modifié ? » — la même question que la vérification de signature d'un
jeton, avec le même outil, appliquée cette fois au corps d'une requête entrante. Reconnaître cette
identité de mécanisme est la moitié de l'exercice ; l'autre moitié tient dans deux pièges propres
aux webhooks.

Le premier piège est la chaîne signée. L'émetteur n'a pas signé le corps seul : il a signé
`horodatage.corps`, liant les deux, et c'est cette chaîne exacte qu'il faut reconstituer avant de
recalculer le condensat. Signer le corps seul — l'erreur intuitive — produit un condensat
différent et refuse tous les envois, même authentiques. Lier l'horodatage à la signature n'est pas
décoratif : c'est ce qui empêche de recoller un vieux corps signé à un horodatage frais, et cela
prépare le contrôle anti-rejeu de l'exercice voisin, où l'horodatage devient la barrière contre le
renvoi d'un envoi capturé.

Le second piège est le corps *brut*. On vérifie sur les octets reçus tels quels, jamais sur une
version désérialisée puis re-sérialisée : la re-sérialisation réordonne les clés, normalise les
espaces, et produit un corps qui ne coïncide plus, au bit près, avec celui que l'émetteur a signé.
C'est le symétrique exact de l'ETag stable — là on voulait une empreinte insensible à la forme,
ici on veut vérifier une empreinte sur la forme *exacte*, et toute reconstruction la casse. La
règle pratique : lire et conserver le corps brut, vérifier, et ne désérialiser qu'ensuite.

Le reste est la discipline commune des vérificateurs de signature. Le condensat se recalcule avec
le secret partagé sur les octets UTF-8 de la chaîne signée. La signature présentée, en
hexadécimal, se décode — et un décodage impossible, comme une longueur incohérente, rend faux sans
exception : un vérificateur reçoit du malformé en permanence, ce n'est pas une erreur de sa part.
La comparaison finale est en temps constant, car l'émetteur — ou un intermédiaire hostile — pourrait
mesurer la durée de réponse pour reconstruire la signature attendue octet par octet.

Le coût est linéaire dans la longueur du corps, dominé par le condensat. La transposition est
l'unité du sujet : signature de jeton, défi PKCE, signature de webhook — c'est à chaque fois le
même triptyque HMAC, comparaison en temps constant, refus silencieux du malformé, avec pour seule
variable *ce que l'on signe*. Ici, la chaîne horodatage-plus-corps ; ailleurs, les segments d'un
jeton. Reconnaître le motif, c'est cesser d'apprendre trois mécanismes pour n'en réviser qu'un.
