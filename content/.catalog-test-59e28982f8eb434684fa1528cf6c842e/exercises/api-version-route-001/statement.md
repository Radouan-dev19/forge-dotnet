# Résoudre la version d'une route

Implémentez `Submission.ResolveApiVersion(string requestPath, string supportedVersions,
string defaultVersion)`.

La version est portée par le premier segment du chemin, sous la forme `v` suivi de chiffres —
`/v2/orders/42`. La méthode décide quelle version servir.

Règles exactes :

- le chemin se découpe en segments non vides ; les séparateurs de bord sont ignorés ;
- si le premier segment a la forme d'une version (`v` puis un ou plusieurs chiffres, sans casse) :
  - normalisé en minuscules, s'il figure dans `supportedVersions` (liste séparée par des
    virgules), rendez-le ;
  - sinon, rendez `"unsupported"` — une version demandée qui n'existe pas ne se rabat pas en
    silence ;
- si le premier segment n'a pas la forme d'une version — ou si le chemin est vide ou racine —,
  rendez `defaultVersion` : aucune version demandée, on sert le défaut ;
- les comparaisons de versions sont insensibles à la casse.

Écrivez avant le code : une version prise en charge, une version inconnue, un chemin sans version,
et un chemin racine.

Exemple : entrée `["/v2/orders/42", "v1,v2,v3", "v3"]`, sortie `"v2"`.
