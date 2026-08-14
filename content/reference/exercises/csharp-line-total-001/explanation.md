# Explication

Un prix fois une quantité : difficile de faire plus court, et pourtant trois décisions d'argent
se cachent dans cette ligne, et chacune a déjà coûté de vrais euros à de vrais systèmes.

La première est le type, décidé en amont par la signature : `decimal`, jamais `double`. Le
binaire flottant ne représente pas exactement les dixièmes et centièmes — un dixième en `double`
est une approximation, et les approximations s'additionnent jusqu'au centime visible sur une
facture. `decimal` calcule en base dix, celle des prix, et représente exactement ce que le
client lit. La règle tient en une phrase : l'argent se calcule en décimal, le scientifique en
flottant — et elle n'admet pas d'exception par commodité.

La deuxième est la place de l'arrondi : *une fois*, après la multiplication. Le contrat le dit —
« multiplier avant l'unique arrondi » — parce que la version qui arrondit le prix unitaire avant
de multiplier fabrique un écart proportionnel à la quantité : un prix de 1,005 arrondi à 1,01
puis multiplié par mille donne dix euros de trop. L'ordre calcul-puis-arrondi confine l'erreur
d'arrondi au dernier centime du total, quel que soit le volume. Cette discipline — garder la
précision maximale pendant le calcul, formater à la sortie — est la même qui gouverne les taux,
les remises et les conversions de devises.

La troisième est la *règle* d'arrondi, et elle doit être nommée. `MidpointRounding.AwayFromZero`
arrondit le demi-centime vers le haut en valeur absolue — la règle commerciale usuelle — alors
que le comportement par défaut de .NET est l'arrondi bancaire, vers le chiffre pair, conçu pour
équilibrer statistiquement de longues séries. Les deux sont légitimes ; les mélanger dans un
même système, ou laisser le défaut décider sans le savoir, produit des totaux qui diffèrent
d'un centime selon le chemin de code — le ticket client le plus pénible qui soit, car chaque
montant isolé semble juste. Le cas caché posé sur un demi-centime exact départage les règles.

Restent les invariants d'entrée, vérifiés avant tout calcul : prix et quantité négatifs ne
décrivent rien dans ce domaine — un remboursement est une autre opération, pas un prix négatif
glissé dans celle-ci — et lever tôt fait remonter la faute à l'appelant. Zéro, en revanche, est
licite des deux côtés : une ligne gratuite ou une quantité nulle donne un total nul, sans cas
spécial.

Le coût est constant ; l'enjeu n'a jamais été la vitesse. La transposition est l'inventaire de
ces trois décisions — type décimal, arrondi unique en sortie, règle de milieu nommée — à exiger
de tout code qui touche un montant, le sien comme celui d'une revue.
