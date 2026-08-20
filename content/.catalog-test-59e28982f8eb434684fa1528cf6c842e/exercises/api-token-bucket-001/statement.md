# Faire évoluer un seau de jetons

Implémentez `Submission.TokensAfterRequest(int tokensBefore, int capacity, int refilled)`.

Un seau de jetons limite le débit : il se remplit à cadence fixe jusqu'à une capacité, et chaque
appel consomme un jeton. La méthode calcule le nombre de jetons *après* un appel, en tenant compte
de la recharge accumulée depuis le précédent.

Règles exactes :

- `capacity` strictement positive et `refilled` non négatif ; sinon `ArgumentOutOfRangeException` ;
- le disponible avant l'appel est le minimum entre `capacity` et `tokensBefore + refilled` : la
  recharge est **plafonnée à la capacité**, un seau inactif n'accumule pas de crédit illimité ;
- si le disponible est strictement positif, l'appel passe et consomme un jeton : rendez le
  disponible moins un ;
- sinon, l'appel est refusé sans consommation : rendez le disponible inchangé — jamais négatif ;
- effectuez l'addition en 64 bits avant de plafonner, pour absorber une longue inactivité.

Écrivez avant le code : un seau partiellement plein, un seau plein qui reçoit une recharge, un
seau vide sans recharge, et une capacité invalide.

Exemple : entrée `[2, 5, 1]`, sortie `2`.
