# Composer une directive de cache

Implémentez `Submission.CacheDirective(string responseKind, int maxAgeSeconds)`.

La méthode compose l'en-tête `Cache-Control` d'une réponse selon sa nature.

Règles exactes :

- `responseKind` se normalise avant décision — rogné, casse aplanie en minuscules invariantes ;
- `maxAgeSeconds` négatif est une faute d'appel : `ArgumentOutOfRangeException` ;
- si la nature est `sensitive`, rendez `"no-store"` — la sensibilité prime sur tout le reste, et
  aucune durée n'apparaît puisque rien n'est stocké ;
- si la nature est `personal`, rendez `"private, max-age=<n>"` — seul le cache du client final
  peut garder la réponse ;
- si la nature est `public`, rendez `"public, max-age=<n>"` — les caches partagés peuvent la
  servir à tous ;
- toute autre nature rend `"no-store"` par présomption de prudence : une nature inconnue ne se
  met pas en cache.

Écrivez avant le code : une réponse publique, une personnelle, une sensible, et une durée
négative.

Exemple : entrée `["public", 3600]`, sortie `"public, max-age=3600"`.
