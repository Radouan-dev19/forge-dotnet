# Calculer l'état d'interaction d'un champ de formulaire

Implémentez `Submission.FieldState(string field, string interactions)`.

La méthode simule, côté client, la petite machine à états d'un unique champ de formulaire et rend son
état final sous la forme `pristine-untouched-invalid` : trois dimensions indépendantes séparées par
des tirets.

Les règles du champ, dans `field`, sont des jetons séparés par des points-virgules :

- `required` : la valeur doit être non vide pour être valide ;
- `optional` : une valeur vide est valide ;
- `minlen=N` : si la valeur est non vide, sa longueur doit être au moins N. Une valeur vide n'est
  jamais recalée par cette règle.

Les événements, dans `interactions`, sont aussi séparés par des points-virgules et rejoués dans
l'ordre :

- `input=valeur` : remplace la valeur courante par ce qui suit le signe égal ;
- `blur` : marque le champ comme touché ;
- `focus` : n'a aucun effet ;
- `reset` : ramène la valeur à sa valeur initiale, la chaîne vide, et repasse le champ à l'état non
  touché.

La valeur initiale est la chaîne vide.

Les trois dimensions de sortie :

- `pristine` si la valeur courante est vide, sinon `dirty` ;
- `untouched` s'il n'y a eu aucun `blur` depuis le dernier `reset`, sinon `touched` ;
- `valid` si la valeur courante respecte les règles, sinon `invalid`.

Un `field` ou un `interactions` absent lève `ArgumentNullException`.

Écrivez avant le code : un champ requis jamais touché, une saisie valide suivie d'un `blur`, un
`blur` seul, et une saisie annulée par un `reset`.

Exemple : entrée `["required", "input=hello;blur"]`, sortie `"dirty-touched-valid"`.
