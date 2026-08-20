# Imposer l'algorithme de signature

Implémentez `Submission.UsesRequiredAlgorithm(string token, string requiredAlgorithm)`.

L'en-tête d'un jeton JWT — son premier segment, un objet JSON encodé en Base64Url — annonce dans
`alg` l'algorithme censé avoir signé le jeton. Cette annonce vient de l'émetteur du jeton, donc de
l'attaquant dans le cas hostile : votre méthode décide si elle correspond exactement à ce que le
vérificateur exige.

Règles exactes :

- un jeton nul, sans exactement trois segments, dont l'en-tête n'est pas décodable ou n'est pas un
  objet JSON, est refusé par `false`, sans exception ;
- une annonce `alg` absente ou d'un autre type qu'une chaîne est refusée ;
- l'annonce `none` est refusée quelle que soit sa casse — `none`, `None`, `NONE` — même si
  l'appelant l'exigeait ;
- sinon, la réponse est la comparaison stricte, sensible à la casse, entre l'annonce et
  `requiredAlgorithm`.

Les jetons des tests sont fabriqués à la main ; leur signature n'est pas vérifiée dans cet
exercice, seule la décision sur l'en-tête compte. Écrivez avant le code : les casses de `none` à
refuser, et ce que devient un en-tête annonçant `HS384` face à une exigence `HS384`.

Exemple : entrée `["<en-tête HS256>.<charge-utile>.<signature>", "HS256"]`, sortie `true`.
