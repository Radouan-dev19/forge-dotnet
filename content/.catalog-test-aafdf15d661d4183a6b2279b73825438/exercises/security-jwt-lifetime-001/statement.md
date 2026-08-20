# Décider la fenêtre de validité d'un jeton

Implémentez `Submission.IsWithinLifetime(string token, int nowUnixSeconds, int toleranceSeconds)`.

La charge utile d'un jeton JWT borne sa validité par deux revendications en secondes d'époque
Unix : `exp`, l'instant d'expiration, et `nbf`, l'instant de prise d'effet. Deux machines n'ayant
jamais la même heure, la vérification applique une tolérance d'horloge qui élargit la fenêtre des
deux côtés.

Règles exactes :

- un jeton illisible — pas trois segments, charge utile indécodable ou qui n'est pas un objet
  JSON — est refusé par `false`, sans exception ;
- `exp` est obligatoire et numérique : son absence refuse le jeton ;
- l'expiration est stricte : le jeton est valide tant que `nowUnixSeconds` est strictement
  inférieur à `exp + toleranceSeconds` ;
- `nbf` est facultative ; si elle est présente et numérique, le jeton est refusé tant que
  `nowUnixSeconds` est strictement inférieur à `nbf - toleranceSeconds` ; présente mais d'un autre
  type, elle refuse le jeton ;
- effectuez l'arithmétique en 64 bits : les instants d'époque additionnés à une tolérance peuvent
  déborder un entier de 32 bits.

Les jetons des tests sont fabriqués à la main avec des instants fixes ; la signature n'est pas
vérifiée ici. Écrivez avant le code : la valeur de vérité à l'instant exact `exp + tolérance`, et à
l'instant exact `nbf - tolérance`.

Exemple : entrée `["<jeton avec exp>", 1749990000, 30]`, sortie `true`.
