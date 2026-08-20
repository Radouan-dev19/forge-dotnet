# Faire avancer l'automate d'un disjoncteur

Implementez `Submission.CircuitState(string events)`.

Un disjoncteur protege un appel distant. Il vit dans trois etats et vous devez rejouer une trace
d'evenements pour rendre l'etat final atteint.

La trace `events` se decoupe sur le point-virgule. Chaque jeton vaut `ok`, `fail` ou `tick` ;
tout autre jeton est ignore. Au depart, l'etat est `closed` et le compteur d'echecs vaut zero.

Regles exactes :

- sur `fail`, si l'etat est `half-open`, l'etat devient `open` et le compteur retombe a zero ;
  sinon, si l'etat est `closed`, le compteur augmente d'une unite, et s'il atteint trois, l'etat
  devient `open` et le compteur retombe a zero ;
- sur `ok`, si l'etat est `half-open`, l'etat redevient `closed` et le compteur retombe a zero ;
  sinon, si l'etat est `closed`, le compteur retombe a zero ;
- sur `tick`, si l'etat est `open`, l'etat devient `half-open` ;
- l'etat `open` n'ecoute ni `ok` ni `fail` : seul un `tick` l'en sort ;
- une trace nulle leve `ArgumentNullException`.

Ecrivez avant le code : la sequence qui ouvre le circuit, celle qui referme apres un essai
probatoire reussi, et celle qui le rouvre apres un essai probatoire echoue.

Exemple : entree `["fail;fail;fail"]`, sortie `"open"`.
