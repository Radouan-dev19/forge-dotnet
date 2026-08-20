# Résoudre l'origine autorisée d'une requête

Implémentez `Submission.ResolveAllowedOrigin(string requestOrigin, string allowlist,
bool withCredentials)`.

La méthode calcule la valeur de l'en-tête d'origine autorisée qu'un serveur doit renvoyer, à
partir de l'origine reçue, de la liste des origines admises et du fait que la requête porte des
identifiants.

Règles exactes :

- `allowlist` se découpe sur la virgule, segments rognés, vides ignorés ; elle peut contenir le
  joker `*` ;
- si la liste contient le joker **et** que la requête est sans identifiants
  (`withCredentials` faux), rendez `"*"` ;
- sinon, si `requestOrigin` est non vide et figure exactement dans la liste, rendez cette origine
  telle quelle ;
- sinon, rendez la chaîne vide — aucune origine autorisée, le navigateur bloquera ;
- en particulier, avec identifiants, le joker est **interdit** : même si la liste est `*`, une
  requête avec identifiants n'obtient que sa propre origine si elle est nommée dans la liste,
  jamais le joker ;
- les comparaisons d'origines sont exactes (ordinales).

Écrivez avant le code : une origine listée avec identifiants, le joker sans identifiants, le joker
avec identifiants, et une origine hors liste.

Exemple : entrée `["https://app.forge.local", "https://app.forge.local,https://admin.forge.local", true]`,
sortie `"https://app.forge.local"`.
