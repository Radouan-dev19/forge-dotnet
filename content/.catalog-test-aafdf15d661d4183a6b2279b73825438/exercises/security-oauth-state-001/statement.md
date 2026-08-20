# Rendre un verdict sur le state de retour

Implémentez `Submission.StateVerdict(string pendingStates, string consumedStates,
string returnedState)`.

Vous êtes le client à l'instant du retour de redirection. Vous tenez deux registres — les
`state` émis encore en attente, et ceux déjà servis — sous forme de listes séparées par des
virgules. La méthode classe le `state` revenu et rend un verdict textuel.

Règles exactes :

- un retour absent, vide ou blanc rend `"missing"` — la réponse ne se rattache à aucune demande ;
- les registres se découpent sur la virgule, segments rognés, segments vides ignorés ; un
  registre absent vaut un registre vide ;
- si le retour figure parmi les *consommés*, rendez `"replayed"` — même s'il figure aussi parmi
  les attentes : le rejeu prime ;
- sinon, s'il figure parmi les attentes, rendez `"accepted"` ;
- sinon, rendez `"forged"` — un state que ce client n'a jamais émis ;
- toutes les comparaisons sont ordinales et sensibles à la casse.

Les valeurs des tests sont factices. Écrivez avant le code : un retour en attente, le même
retour une fois consommé, un retour jamais émis, et un retour vide.

Exemple : entrée `["st-aaa,st-bbb", "st-old", "st-bbb"]`, sortie `"accepted"`.
