# Explication

Convertir des euros en centimes, c'est changer d'unité — et les changements d'unité sont
l'endroit exact où les montants se corrompent. L'énoncé demande d'expliquer pourquoi convertir
trop tôt en `int` serait faux : c'est le cœur, et il faut le dérouler.

La conversion prématurée — tronquer le montant en entier d'euros, puis multiplier par cent —
jette les décimales avant qu'elles n'aient pu devenir des centimes : dix euros et cinq centimes
deviendraient mille centimes au lieu de mille cinq. Même l'ordre correct mais typé trop tôt —
`(int)(amount * 100)` — échoue plus subtilement : la conversion par transtypage *tronque* vers
zéro au lieu d'arrondir, et un montant qui, multiplié, donne 1000,9999 à cause d'un calcul amont
rendrait mille au lieu de mille un. La chaîne correcte tient en trois temps ordonnés :
multiplier en `decimal`, arrondir *en decimal* avec la règle nommée, convertir en entier en
dernier — quand la valeur est déjà exactement entière et que la conversion ne peut plus rien
détruire. `decimal.ToInt32` sur le résultat arrondi est alors une simple lecture.

La règle d'arrondi est écrite dans le contrat : le demi-centime s'éloigne de zéro, dix euros et
demi-centime rendent mille et un centimes. Le comportement par défaut de .NET — l'arrondi
bancaire vers le pair — rendrait mille : un centime d'écart, invisible en test manuel,
systématique sur les montants en demi-centime. Nommer `MidpointRounding.AwayFromZero` dans
l'appel n'est pas de la pédanterie, c'est le seul moyen d'être sûr que la règle appliquée est
celle du contrat et non celle du framework du moment.

Le montant négatif est refusé avant tout calcul — le domaine annoncé commence à zéro — et la
borne haute du domaine, un million d'euros, garantit que le résultat en centimes tient dans un
`int` : cent millions de centimes, loin des deux milliards du type. Cette vérification-là est
faite *par le contrat*, pas par le code, et c'est un choix à savoir lire : quand le domaine
d'entrée est borné par l'énoncé, le code peut s'appuyer dessus ; quand il ne l'est pas, la
conversion finale devrait elle-même être vérifiée.

Pourquoi des centimes entiers, d'ailleurs ? Parce que c'est la représentation qui rend les
comparaisons et les cumuls exacts par construction — beaucoup de systèmes de paiement stockent
des entiers de plus petite unité précisément pour cela. Le coût est constant ; la transposition
est chaque frontière d'unité — euros vers centimes, heures vers minutes, mètres vers
millimètres : multiplier dans le type large, arrondir avec une règle nommée, convertir en
dernier. Dans cet ordre, toujours.
