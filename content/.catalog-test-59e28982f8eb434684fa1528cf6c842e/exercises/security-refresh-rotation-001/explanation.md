# Explication

La rotation des jetons de rafraîchissement a une conséquence arithmétique discrète : chaque
nouveau jeton doit recevoir une durée, et cette durée obéit à deux horloges qui ne se négocient
pas de la même façon. L'exercice isole ce calcul — deux comparaisons, un minimum — parce que
c'est lui qui décide si une session peut durer éternellement.

La durée de glissement d'abord : c'est la fenêtre normale entre deux rotations — l'utilisateur
actif voit sa session prolongée à chaque usage, et c'est le confort attendu. Mais un glissement
seul crée la session immortelle : tant qu'on l'utilise, elle ne meurt jamais, et un jeton volé
activement exploité se renouvelle indéfiniment. D'où la seconde horloge : l'échéance *absolue*
de la session, fixée à l'ouverture, qu'aucune rotation ne repousse. La règle de composition est
un minimum : le prochain jeton vit la durée de glissement, ou le reste de session si celui-ci
est plus court. Les dernières fenêtres d'une session rétrécissent donc naturellement — c'est le
comportement voulu, et l'erreur classique le casse : rendre toujours la durée de glissement fait
déborder la dernière fenêtre au-delà de l'échéance absolue, et la promesse « cette session ne
dépassera jamais telle date » devient fausse précisément à la fin, là où elle compte.

Le régime des bornes distingue deux situations que le contrat sépare soigneusement. La session
finie — l'instant courant a atteint l'échéance — rend zéro : c'est un état *ordinaire* du cycle
de vie, celui que le guichet traduit en refus de rafraîchir, et le traiter par exception
transformerait chaque fin de session en incident. La durée de glissement non positive, en
revanche, est une faute de l'appelant — une configuration qui ne décrit aucune politique — et
elle lève. La frontière de l'échéance est atteinte *inclusivement* : à l'instant exact, zéro —
le jeton de la dernière seconde n'existe pas — et le cas caché posé dessus fige l'inclusivité,
avec son voisin une seconde avant qui rend une fenêtre de un.

L'arithmétique se fait en 64 bits : l'écart entre deux instants d'époque de signes opposés
déborde un entier de 32 bits, et un reste de session devenu négatif par enroulement passerait le
minimum sans bruit. Le résultat, lui, tient toujours dans le type de retour — borné par la durée
de glissement, qui est un entier.

Le coût est constant. La transposition dépasse les jetons : bails de verrous distribués,
sessions de caisse, délégations temporaires — partout où un droit se renouvelle par usage, la
même paire d'horloges s'impose : une fenêtre glissante pour le confort, un plafond absolu pour
la sécurité, et un minimum qui les compose. Le système qui n'a que la première horloge a promis
sans le savoir des sessions éternelles.
