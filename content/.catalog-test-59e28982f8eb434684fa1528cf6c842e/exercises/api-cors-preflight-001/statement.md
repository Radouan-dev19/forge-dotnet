# Décider l'issue d'un préflight

Implémentez `Submission.IsPreflightAllowed(string requestedMethod, string allowedMethods,
string requestedHeaders, string allowedHeaders)`.

Le navigateur envoie un préflight avant une requête à effet potentiel : il annonce la méthode et
les en-têtes qu'il *voudrait* utiliser, et attend que le serveur confirme les deux. La méthode
rend vrai si le préflight passe.

Règles exactes :

- les listes se découpent sur la virgule, segments rognés, vides ignorés ;
- la méthode demandée doit figurer dans `allowedMethods` — comparaison insensible à la casse ;
- *chaque* en-tête de `requestedHeaders` doit figurer dans `allowedHeaders` — noms d'en-têtes
  comparés sans casse ; un seul en-tête demandé absent de la liste autorisée suffit à refuser ;
- une liste d'en-têtes demandés vide n'est pas un refus : c'est une absence d'exigence, le
  préflight peut passer sur la seule méthode ;
- l'inclusion est à sens unique : tous les *demandés* doivent être *autorisés*, mais la liste
  autorisée peut contenir davantage.

Écrivez avant le code : méthode et en-têtes tous autorisés, un en-tête demandé non autorisé, une
méthode non autorisée, et aucun en-tête demandé.

Exemple : entrée `["PUT", "GET,PUT,DELETE", "content-type,authorization",
"content-type,authorization,x-trace"]`, sortie `true`.
