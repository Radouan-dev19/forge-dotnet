# Explication

Le budget d'erreur restant et la vitesse de combustion répondent à deux questions différentes, et les
confondre produit les deux pathologies d'alerte classiques. Le restant dit « combien il reste » —
utile pour gouverner les livraisons, inutile pour l'astreinte : un budget à moitié plein se vide en
douceur ou en chute libre, et le nombre seul ne distingue pas. La vitesse dit « à quel rythme ça
part » : c'est elle qui sépare l'incident du bruit de fond, et c'est sur elle que se bâtissent les
alertes qui réveillent à bon escient.

**Pourquoi le rapport des taux, et pas le taux d'échec.** Le taux d'échec observé — deux dixièmes de
pour cent — ne dit rien sans son référentiel : sous un objectif de deux neufs il est confortable, sous
quatre neufs il est vingt fois trop haut. Diviser par le taux toléré normalise : la vitesse de un est
la même alerte quel que soit l'objectif, et les seuils d'alerte deviennent portables d'un service à
l'autre — c'est la propriété qui permet une politique d'alerte d'équipe au lieu d'un réglage par
service. La lecture opérationnelle est directe : à vitesse quatorze, un budget mensuel part en un peu
plus de deux jours ; à vitesse un, il dure exactement le mois.

**Pourquoi les alertes sérieuses croisent deux fenêtres.** Une fenêtre courte seule sonne sur chaque
pic — la rafale de trente secondes affiche une vitesse énorme puis retombe ; une fenêtre longue seule
sonne trop tard — l'incident d'une heure se dilue dans la journée. Exiger la vitesse haute sur la
fenêtre courte **et** une vitesse encore élevée sur la fenêtre longue conjugue les deux : l'intensité
et la persistance. C'est le même arbitrage que l'alerte de persistance du socle, exprimé dans l'unité
du budget.

**Pourquoi l'objectif parfait se refuse ici.** À cent pour cent, le taux toléré est nul : la vitesse
serait une division par zéro, et aucune valeur de remplacement n'est honnête — l'infini ne se
plancher pas. Ce n'est pas une lacune : une fenêtre critique gouvernée à l'objectif parfait se
surveille au budget restant — zéro toléré, tout échec est un dépassement — et la vitesse n'y ajoute
aucune information. Refuser le calcul dit exactement cela.

**Le décimal et le plancher, encore et pour les mêmes raisons.** Les objectifs sont des décimaux sans
représentation binaire exacte, et le centième du rapport dériverait selon la fenêtre ; le plancher
garde le chiffre du côté sûr — une combustion affichée à un virgule zéro zéro n'est jamais en
réalité à zéro virgule neuf-neuf-six arrondie vers le haut, ce qui compte quand le seuil d'alerte est
exactement un.

En entretien, le terme est burn rate, et la question type porte sur les alertes multi-fenêtres —
pourquoi deux fenêtres, et pourquoi des seuils différents sur chacune. La réponse tient dans le
paragraphe ci-dessus : intensité et persistance.
