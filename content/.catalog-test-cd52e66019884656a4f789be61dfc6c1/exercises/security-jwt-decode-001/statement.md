# Lire une revendication dans un jeton

Implémentez `Submission.ReadClaim(string token, string claim)`.

Un jeton JWT se compose de trois segments séparés par des points ; le deuxième est la charge utile,
un objet JSON encodé en Base64Url. Votre méthode décode cette charge utile à la main et retourne la
revendication demandée.

Règles exactes :

- un jeton qui ne porte pas exactement trois segments lève `ArgumentException` ;
- Base64Url remplace `+` par `-` et `/` par `_`, et supprime le remplissage final : restaurez les
  deux avant de décoder, et levez `ArgumentException` si la longueur restante rend le décodage
  impossible (reste de un modulo quatre) ou si le contenu n'est pas du JSON objet ;
- si la revendication demandée est absente, retournez la chaîne vide ;
- si sa valeur JSON est une chaîne, retournez cette valeur ; pour tout autre type (nombre, tableau…),
  retournez son texte JSON brut.

Les jetons des tests sont fabriqués à la main avec le secret factice `forge-fake-jwt-secret` : la
signature n'est pas vérifiée ici, seul le décodage compte. Écrivez avant le code : une charge utile
dont la longueur encodée exige zéro, un ou deux caractères de remplissage, et une revendication
accentuée.

Exemple : entrée `["<en-tête>.<charge-utile>.<signature>", "sub"]`, sortie `"user-482"`.
