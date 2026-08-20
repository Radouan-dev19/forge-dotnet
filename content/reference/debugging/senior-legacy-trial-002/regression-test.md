# Non-regression

Ajoutez, et gardez, un cas de caracterisation sur un **essai deja expire** : une consultation
posterieure a la fin de la periode doit rendre zero, jamais un reste negatif.

- Un essai de 14 jours commence le 1er mars, consulte le 20 mars, rend 0.
- Le meme essai consulte le 15 mars, jour exact de l'expiration, rend 0.
- Le meme essai consulte le 10 mars rend 5.

Gardez aussi les cas aux bornes qui doivent rester stables : la consultation du premier jour rend la
longueur entiere, une longueur d'essai inferieure a 1 leve une exception d'argument, une date du jour
anterieure au debut aussi. Le plancher a zero est la propriete que ces cas figent.
