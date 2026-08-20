# Decider l'action d'un consommateur de messages

Implementez `Submission.ConsumerAction(int deliveryCount, string messageId, string processedIds)`.

Un consommateur retire un message d'une file. Trois informations le guident : le nombre de fois
que ce message a deja ete livre, son identifiant, et la liste des identifiants deja traites avec
succes. La methode rend l'action a mener.

L'argument `processedIds` liste les identifiants deja traites, separes par la virgule ; il peut
etre vide. La comparaison se fait par jeton exact contre cet ensemble.

Regles exactes, appliquees dans cet ordre :

- si `deliveryCount` est strictement inferieur a un, levez `ArgumentOutOfRangeException` ;
- sinon, si `deliveryCount` est strictement superieur a cinq, rendez `dead-letter` ;
- sinon, si `messageId` figure exactement dans l'ensemble des identifiants deja traites, rendez
  `ack-duplicate` ;
- sinon, rendez `process`.

La premiere regle qui s'applique decide ; les suivantes ne sont plus examinees. C'est pourquoi un
message livre trop de fois part en `dead-letter` meme s'il figure parmi les traites.

Ecrivez avant le code : un premier traitement, un doublon deja traite, et un message livre au-dela
de la limite.

Exemple : entree `[1, "m1", ""]`, sortie `"process"`.
