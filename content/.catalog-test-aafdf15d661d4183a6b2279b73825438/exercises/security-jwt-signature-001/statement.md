# Vérifier la signature HMAC d'un jeton

Implémentez `Submission.IsSignatureValid(string token, string secret)`.

La signature d'un jeton JWT signé en HMAC-SHA256 est le condensat de la chaîne
`en-tête.charge-utile` — les deux premiers segments encodés, avec le point qui les sépare —
calculé avec la clé secrète, puis encodé en Base64Url dans le troisième segment. Votre méthode
recalcule ce condensat avec la clé reçue et le compare à celui que le jeton présente.

Règles exactes :

- un jeton nul, sans exactement trois segments, ou dont le troisième segment n'est pas du
  Base64Url décodable, est refusé par `false` — jamais par une exception ;
- l'algorithme est HMAC-SHA256, imposé par votre code ; l'en-tête du jeton n'est pas consulté ;
- la comparaison des deux condensats utilise `CryptographicOperations.FixedTimeEquals`.

Les jetons des tests sont fabriqués à la main avec le secret factice `forge-fake-jwt-secret` ;
certains sont signés avec une autre clé factice, ou falsifiés après signature. Écrivez avant le
code : ce que devient la vérification si la charge utile a changé d'un caractère, et si la
signature a été tronquée.

Exemple : entrée `["<en-tête>.<charge-utile>.<signature correcte>", "forge-fake-jwt-secret"]`,
sortie `true`.
