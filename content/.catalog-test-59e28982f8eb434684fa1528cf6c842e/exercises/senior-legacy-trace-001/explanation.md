# Explication

Cet exercice entraine la competence qui manque le plus au parcours : lire et comprendre du code que
l'on n'a pas ecrit, sans documentation, en reconstituant sa regle a partir de son comportement. Le
piege est le mot `void`, dont le nom n'annonce ni credit ni debit et dont la semantique ne se devine
pas : elle se deduit des cas. C'est exactement la posture d'un developpeur qui reprend une base
existante et doit formuler une hypothese avant de toucher quoi que ce soit.

La regle, une fois reconstituee, est claire. Le solde part de zero. Un credit ajoute son montant, un
debit le retranche. Le `void` annule l'effet de la **derniere entree appliquee** : il retranche du
solde exactement ce que cette entree y avait ajoute. Sur `credit:50;void`, le credit porte le solde a
cinquante, puis le void retranche ces cinquante et ramene a zero. Sur `credit:10;debit:5;void`, le
solde passe a dix puis a cinq ; le void annule le dernier effet, un debit de cinq, donc il rajoute
cinq et le solde revient a dix. Ce dernier cas est celui qui tranche entre une bonne comprehension et
une fausse : qui croit que `void` remet le solde a zero se trompe, et qui empile le montant brut au
lieu de l'effet signe annule le debit dans le mauvais sens.

Le noyau decidable tient deux etats en parcourant les entrees : le solde courant, et une pile des
effets deja appliques. Chaque credit ou debit pousse son effet signe sur la pile ; un `void` depile
le dernier effet et le retranche du solde. Une pile permet d'enchainer plusieurs `void` successifs,
qui defont les entrees dans l'ordre inverse, comme le montrent les cas caches. La structure n'est pas
un detail : c'est elle qui rend la regle correcte au-dela d'un seul niveau d'annulation.

Les cas caches eprouvent les bornes que la lecture rapide manque. Un `void` isole ne doit rien
casser : il n'y a rien a annuler, et le faire echouer serait plus strict que le code hérité. Deux
`void` a la suite doivent defaire les deux dernieres entrees, ce qui n'est possible qu'avec une pile
et non une simple memoire du dernier effet. Une entree dont le montant est illisible, ou dont le type
n'est ni credit ni debit ni void, est une donnee corrompue : lever une exception d'argument vaut mieux
que de produire un solde faux sur une entree que l'on n'a pas su lire.

Le cout d'une erreur sur du legacy est particulier : le code fonctionne deja en production, donc un
contresens sur `void` produit un solde faux qui ressemble a un solde juste. C'est pourquoi la methode
consiste a ecrire des tests de caracterisation qui figent le comportement observe avant d'y toucher,
plutot qu'a supposer l'intention de l'auteur. Cette discipline, formuler une hypothese, la prouver sur
des cas, puis seulement agir, est la meme que celle des laboratoires de debogage, appliquee ici a une
base que l'apprenant decouvre.
