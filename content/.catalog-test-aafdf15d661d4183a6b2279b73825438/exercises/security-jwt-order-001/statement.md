# Enchaîner la validation dans l'ordre imposé

Implémentez `Submission.FirstRejection(string token, string secret, string expectedIssuer,
string expectedAudience, int nowUnixSeconds)`.

Votre méthode exécute la chaîne complète de validation d'un jeton et retourne le verdict du
premier contrôle qui échoue — ou `"valid"` si tous passent. Les contrôles s'enchaînent dans cet
ordre, et lui seul :

1. `"format"` : le jeton porte trois segments ; l'en-tête et la charge utile se décodent en
   Base64Url vers des objets JSON ; la signature se décode en Base64Url ;
2. `"algorithm"` : l'en-tête annonce `alg` sous forme de chaîne strictement égale à `HS256` ;
3. `"signature"` : le condensat HMAC-SHA256 de `en-tête.charge-utile`, calculé avec `secret`, est
   égal — comparaison en temps constant — à la signature du jeton ;
4. `"expiration"` : la charge utile porte un `exp` numérique et `nowUnixSeconds` lui est
   strictement inférieur — aucune tolérance dans cet exercice ;
5. `"issuer"` : la revendication `iss` est une chaîne strictement égale à `expectedIssuer` ;
6. `"audience"` : la revendication `aud`, chaîne ou tableau de chaînes, contient strictement
   `expectedAudience`.

Un jeton nul est un échec de format. Aucune exception ne sort de la méthode.

Les jetons des tests sont fabriqués à la main avec le secret factice `forge-fake-jwt-secret` et
des instants fixes. Écrivez avant le code : le verdict d'un jeton expiré dont la signature est
falsifiée, et celui d'un jeton dont l'émetteur et l'audience sont tous deux faux.

Exemple : entrée `["<jeton conforme>", "forge-fake-jwt-secret", "forge-issuer", "forge-api", 1749990000]`, sortie `"valid"`.
