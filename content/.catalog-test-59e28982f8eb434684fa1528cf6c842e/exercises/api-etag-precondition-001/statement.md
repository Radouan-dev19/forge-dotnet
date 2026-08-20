# Décider le statut d'une requête conditionnelle

Implémentez `Submission.PreconditionOutcome(string method, string currentETag,
string ifNoneMatch, string ifMatch)`.

La méthode rend le code de statut d'une requête conditionnelle, selon qu'il s'agit d'une lecture
ou d'une écriture.

Règles exactes :

- si `method` est `"GET"` ou `"HEAD"` (sans casse), c'est une **lecture**, gouvernée par
  `ifNoneMatch` :
  - si `ifNoneMatch` égale `currentETag`, rendez `304` — le client a déjà l'état courant ;
  - sinon, rendez `200` — on envoie la représentation ;
- sinon c'est une **écriture**, gouvernée par `ifMatch` :
  - si `ifMatch` est absent ou vide, rendez `428` — une écriture sans condition est refusée pour
    ne pas rouvrir la mise à jour perdue ;
  - si `ifMatch` égale `currentETag`, rendez `200` — l'état n'a pas bougé, l'écriture procède ;
  - sinon, rendez `412` — l'état a changé depuis, l'écriture est refusée ;
- les comparaisons d'empreintes sont exactes, guillemets compris.

Écrivez avant le code : une lecture à jour, une lecture périmée, une écriture concurrente, et une
écriture sans condition.

Exemple : entrée `["GET", "\"abc\"", "\"abc\"", ""]`, sortie `304`.
