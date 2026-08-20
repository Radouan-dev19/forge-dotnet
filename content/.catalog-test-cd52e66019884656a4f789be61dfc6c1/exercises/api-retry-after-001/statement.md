# Calculer le Retry-After d'une fenêtre fixe

Implémentez `Submission.RetryAfterSeconds(int requestCount, int quota, int nowUnix,
int windowSeconds, int windowStartUnix)`.

Une limite à fenêtre fixe autorise `quota` appels par tranche de `windowSeconds`, à partir de
`windowStartUnix`. La méthode calcule le `Retry-After`, en secondes, à indiquer au client.

Règles exactes :

- `windowSeconds` strictement positif ; sinon `ArgumentOutOfRangeException` ;
- si `requestCount` est strictement inférieur à `quota`, le client n'est pas limité : rendez `0`,
  il n'a rien à attendre ;
- sinon, il est limité jusqu'à la réinitialisation : rendez le nombre de secondes de l'instant
  courant jusqu'au **début de la fenêtre suivante** — `windowStartUnix + windowSeconds` ;
- ce délai ne peut jamais être négatif : si l'instant courant a dépassé la réinitialisation,
  rendez `0` ;
- effectuez l'arithmétique d'instants en 64 bits.

Écrivez avant le code : un client sous son quota, un client au quota en début de fenêtre, le même
en fin de fenêtre, et une fenêtre déjà dépassée.

Exemple : entrée `[5, 5, 1749990012, 60, 1749990000]`, sortie `48`.
