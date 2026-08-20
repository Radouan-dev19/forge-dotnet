# Valider les revendications propres d'un jeton d'identité

Implémentez `Submission.IdTokenVerdict(string idToken, string expectedNonce,
string expectedClientId, string accessToken)`.

Vous êtes le client à la réception de la réponse de jetons. La signature, l'émetteur, l'audience
et l'échéance du jeton d'identité sont supposés déjà vérifiés par la chaîne de la semaine
quatorze ; votre méthode enchaîne les trois contrôles *propres* à l'identité et rend le verdict
du premier qui échoue :

1. `"format"` : le jeton porte trois segments et sa charge utile se décode en objet JSON —
   Base64Url, remplissage restauré ;
2. `"nonce"` : la revendication `nonce` existe, est une chaîne, et vaut exactement
   `expectedNonce` ;
3. `"azp"` : la revendication `azp` existe, est une chaîne, et vaut exactement
   `expectedClientId` ;
4. `"at-hash"` : la revendication `at_hash` existe et vaut l'empreinte du jeton d'accès reçu
   avec lui — moitié gauche (seize octets) du condensat SHA-256 des octets ASCII du jeton
   d'accès, encodée en Base64Url sans remplissage ;
5. sinon `"valid"`.

Les comparaisons sont ordinales et sensibles à la casse. Aucune exception ne sort de la méthode.
Les jetons des tests sont fabriqués à la main avec des valeurs factices. Écrivez avant le code :
un jeton dont seule l'empreinte diverge, et un jeton sans revendication de nonce.

Exemple : entrée `["<jeton d'identité>", "n-1a2b", "orders-web", "<jeton d'accès>"]`, sortie
`"valid"`.
