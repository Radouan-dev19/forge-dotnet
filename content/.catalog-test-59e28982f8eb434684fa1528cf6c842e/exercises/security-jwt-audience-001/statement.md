# Contrôler l'audience d'un jeton

Implémentez `Submission.IsForAudience(string token, string expectedAudience)`.

La revendication `aud` de la charge utile nomme le ou les services auxquels le jeton est destiné.
La norme lui permet deux formes JSON : une chaîne unique, ou un tableau de chaînes quand le jeton
vise plusieurs services. Votre méthode décide si le jeton est destiné au service attendu.

Règles exactes :

- un jeton illisible — pas trois segments, charge utile indécodable ou qui n'est pas un objet
  JSON — est refusé par `false`, sans exception ;
- une revendication `aud` absente est un refus : un jeton sans destinataire déclaré n'est destiné
  à personne ;
- si `aud` est une chaîne, la réponse est son égalité stricte, sensible à la casse, avec
  `expectedAudience` ;
- si `aud` est un tableau, la réponse est vraie dès qu'un de ses éléments de type chaîne
  correspond strictement ; les éléments d'un autre type sont ignorés ;
- toute autre forme JSON de `aud` — nombre, objet, booléen — est un refus.

Les jetons des tests sont fabriqués à la main ; la signature n'est pas vérifiée dans cet exercice.
Écrivez avant le code : le verdict pour un tableau vide, et pour une audience qui ne diffère que
par la casse.

Exemple : entrée `["<jeton aud=forge-api>", "forge-api"]`, sortie `true`.
