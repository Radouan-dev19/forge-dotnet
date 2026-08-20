# Vérifier un défi PKCE S256

Implémentez `Submission.IsValidPkce(string codeVerifier, string codeChallenge)`.

Vous êtes le serveur d'autorisation à l'instant de l'échange : le client présente son secret —
le `code_verifier` — et vous détenez l'empreinte déposée à l'aller — le `code_challenge`. La
méthode répond vrai si le secret correspond à l'empreinte selon la méthode S256.

Règles exactes :

- le secret est refusé (`false`, sans exception) s'il est absent, plus court que 43 caractères,
  plus long que 128, ou s'il contient un caractère hors de l'alphabet non réservé : lettres,
  chiffres, tiret, point, souligné, tilde ;
- l'empreinte est refusée si elle est absente ou vide ;
- l'empreinte attendue est le condensat SHA-256 des octets ASCII du secret, encodé en Base64Url
  sans remplissage — `+` devient `-`, `/` devient `_`, les `=` finaux disparaissent ;
- la comparaison des deux empreintes se fait en temps constant, sur leurs octets.

Les secrets des tests sont factices et fabriqués à la main. Écrivez avant le code : un couple
conforme aux deux bornes exactes de longueur, un secret d'un caractère trop court, et une
empreinte à laquelle on aurait laissé son remplissage.

Exemple : entrée `["<secret de 43 caractères>", "<son empreinte S256>"]`, sortie `true`.
