# Décider si une réponse est stockable en cache

Implémentez `Submission.IsStorable(string method, int statusCode)`.

Avant même de composer une directive, un cache doit savoir si une réponse *peut* être stockée.
La méthode répond par oui ou non selon la méthode HTTP et le statut.

Règles exactes :

- `method` se normalise — rogné, majuscules invariantes ;
- seules les *lectures sans effet* sont stockables : `GET` et `HEAD` ; toute autre méthode —
  `POST`, `PUT`, `DELETE`, `PATCH`, ou une méthode inconnue — rend `false`, car resservir la
  réponse d'une action à une action qui n'a pas eu lieu n'a aucun sens ;
- sur une lecture, seuls certains statuts sont stockables : `200`, `203`, `204`, `301`, `404`,
  `410` ; tout autre statut — notamment les erreurs de serveur et les réponses conditionnelles —
  rend `false` ;
- les deux conditions sont cumulatives : lecture *et* statut stockable.

Écrivez avant le code : une lecture réussie, une lecture en erreur, une écriture réussie, et une
méthode inconnue.

Exemple : entrée `["GET", 200]`, sortie `true`.
