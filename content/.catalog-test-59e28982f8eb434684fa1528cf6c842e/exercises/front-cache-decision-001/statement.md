# Décider la fraîcheur d'une entrée en cache

Implémentez
`Submission.CacheDecision(string entry, int nowSeconds, int staleAfter, int expireAfter)`.

La méthode applique la stratégie « servir périmé pendant la revalidation » (stale-while-revalidate)
à une entrée de cache et rend l'un de trois verdicts exacts : `"fresh"`, `"stale-revalidate"` ou
`"expired"`.

L'entrée est décrite par `entry`, de la forme `storedAt=N`, où N est l'instant de stockage en
secondes. L'âge de l'entrée est `nowSeconds - storedAt`.

Les trois zones, sur cet âge, sont délimitées par des bornes exactes :

- un âge strictement inférieur à `staleAfter` : `"fresh"` ;
- un âge compris entre `staleAfter` inclus et `expireAfter` exclu : `"stale-revalidate"` ;
- un âge supérieur ou égal à `expireAfter` : `"expired"`.

Un âge négatif, dû à une horloge décalée entre l'écriture et la lecture, tombe naturellement dans la
première zone et vaut donc `"fresh"`.

La méthode valide ses entrées avant de décider. Elle lève une exception d'argument
(`ArgumentException` ou `ArgumentOutOfRangeException`) si l'un de ces points est en défaut :

- `entry` n'est pas de la forme `storedAt=N` avec N entier ;
- `staleAfter` est négatif, ou `expireAfter` est négatif ;
- `staleAfter` dépasse `expireAfter`, ce qui rendrait la zone intermédiaire impossible.

Écrivez avant le code : un âge nettement sous le seuil, un âge pile égal à `staleAfter`, un âge pile
égal à `expireAfter`, et un couple de seuils incohérents.

Exemple : entrée `["storedAt=1000", 1040, 30, 120]`, sortie `"stale-revalidate"`.
