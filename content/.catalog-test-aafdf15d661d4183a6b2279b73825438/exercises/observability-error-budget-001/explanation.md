# Explication

Le budget d'erreur est l'idée qui réconcilie deux camps condamnés à se disputer : ceux qui veulent
livrer et ceux qui veulent que rien ne casse. Tant que la fiabilité s'exprime en pourcentage, elle
reste un vœu ; convertie en nombre d'échecs dépensables sur une fenêtre, elle devient une monnaie
commune — on peut la dépenser en déploiements audacieux quand il en reste, on gèle les risques quand
elle s'épuise. Encore faut-il que la conversion soit irréprochable, et c'est tout l'objet des trois
choix que cet exercice impose.

**Le plancher, parce que l'arrondi est une porte dérobée.** La part tolérée appliquée à une fenêtre
donne rarement un entier : 999 requêtes à trois neufs tolèrent 0,999 échec. Arrondir au plus proche
donnerait un ; arrondir vers le bas donne zéro. La différence semble pédante jusqu'à ce qu'on la lise
comme une promesse : l'objectif dit qu'au plus un millième des requêtes peut échouer, et un échec sur
999 fait plus d'un millième. Le plancher est le seul arrondi qui ne dépasse jamais la promesse ; tout
autre choix fabrique, sur les petites fenêtres, un budget que l'objectif ne concède pas — et les
petites fenêtres sont précisément celles des alertes rapides.

**Le décimal, parce que les objectifs sont des nombres décimaux.** Les valeurs 99,9 ou 99,95 n'ont
pas de représentation exacte en flottant binaire ; l'erreur est infime, mais elle vit exactement là où
le plancher tranche. Une multiplication qui rend 9,999999 au lieu de 10 fait perdre une unité de
budget ; l'inverse en fait gagner une. Le type décimal représente ces valeurs exactement, et le calcul
devient reproductible d'une machine à l'autre — une propriété qu'un chiffre de gouvernance ne peut pas
négocier.

**Le négatif, parce que le dépassement est une information.** Écrêter le restant à zéro paraît
raisonnable — « il n'y a plus de budget » — et détruit la moitié du signal. Un budget à moins deux et
un budget à moins deux cents appellent des réponses différentes : le premier se rattrape en gelant un
déploiement, le second déclenche une revue d'incident. Le nombre négatif chiffre la dette de
fiabilité ; le zéro écrêté la cache au moment précis où il faut la connaître.

**Les refus dessinent le domaine réel.** Des échecs supérieurs au volume ou des comptes négatifs ne
décrivent aucune fenêtre possible — c'est une télémétrie corrompue, et la chiffrer produirait un
budget d'apparence sérieuse sur des données absurdes. L'objectif de cent pour cent, lui, est accepté :
un budget nul est exigeant mais cohérent, et certaines fenêtres critiques — paiements, sécurité — se
gouvernent réellement ainsi.

La transposition : tout quota — appels d'interface, dépenses d'infonuagique, temps d'indisponibilité
planifié — se gère avec la même mécanique : allocation plancher, consommation soustraite, découvert
visible.
