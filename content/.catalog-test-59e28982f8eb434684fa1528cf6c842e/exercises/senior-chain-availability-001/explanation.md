# Explication

La disponibilité composée est le calcul qui manque à la plupart des schémas d'architecture : chaque
boîte y affiche fièrement ses neufs, et personne ne multiplie. Or la multiplication est toute la
physique du problème — et ses conséquences renversent plusieurs intuitions confortables.

**Pourquoi le produit et rien d'autre.** Une requête synchrone traverse tous les maillons : elle
réussit si le premier réussit **et** le deuxième **et** le troisième. Des probabilités qui doivent
toutes se réaliser se multiplient. Le minimum de la chaîne — l'intuition la plus répandue —
supposerait que les pannes des différents maillons coïncident toujours, ce qui est exactement
l'inverse de la réalité : des services indépendants tombent à des moments indépendants, et leurs
indisponibilités s'additionnent presque. La moyenne, elle, n'a aucune interprétation : elle monte
quand on ajoute un maillon médiocre à une chaîne pire que lui.

**Ce que le produit enseigne sur l'architecture.** Deux conséquences se lisent directement dans la
formule. D'abord, chaque maillon synchrone ajouté **coûte**, même excellent : un maillon à trois
neufs retire un dixième de pour cent à tout ce qu'il rejoint — huit heures par an. C'est l'argument
chiffré du découplage asynchrone : une file entre deux services sort le second du produit, parce que
la requête n'attend plus sa réussite. Ensuite, la chaîne nivelle par le bas mais pas seulement : cinq
maillons à deux neufs font moins que 95,1 % — aucun maillon n'est mauvais, l'ensemble l'est. La
limite de dix maillons du contrat encode cette leçon : au-delà, le chiffre n'informe plus, il
constate — et la réponse n'est pas un calcul mais une refonte.

**Pourquoi le décimal exact et le plancher final.** Les pourcentages de disponibilité — 99,9,
99,95 — n'ont pas de représentation binaire exacte, et l'erreur de représentation, insignifiante par
maillon, se cumule au fil du produit : le centième final peut dériver selon l'ordre des maillons, ce
qui est indéfendable pour un chiffre contractuel. Le décimal supprime la dérive. Quant au plancher,
il découle de ce que le chiffre **promet** : une disponibilité annoncée est un engagement vers le
bas — « au moins ceci ». L'arrondi au plus proche transformerait 99,304 en 99,3 mais aussi 99,296 en
99,3, c'est-à-dire promettrait un centième que la chaîne ne tient pas. Et l'arrondi ne se fait qu'une
fois, à la fin : arrondir à chaque maillon accumule des faveurs que personne n'a consenties.

**Le cas des maillons parfaits calibre l'intuition.** Deux maillons à cent pour cent composent cent
pour cent : le produit est neutre pour l'élément neutre, et c'est le seul cas où ajouter un maillon
est gratuit. Le troisième maillon à 99,9 ramène l'ensemble à 99,9 — la chaîne vaut exactement son
maillon imparfait, tant qu'il est seul.

En entretien, ce calcul accompagne toute discussion d'architecture distribuée : c'est lui qui répond
à « pourquoi pas un service de plus ? » avec un nombre au lieu d'un sentiment.
