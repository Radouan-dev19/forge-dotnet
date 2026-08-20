# Explication

Les percentiles sont au cœur de tous les objectifs de latence, et pourtant la plupart des ingénieurs
les consomment sans jamais en avoir calculé un à la main. L'exercice comble ce trou, parce que les
trois choix de la méthode — le rang plutôt que l'interpolation, le plafond plutôt que l'arrondi, la
copie plutôt que le tri en place — portent chacun une leçon qui dépasse le calcul.

**Pourquoi la moyenne ment et le percentile non.** La moyenne agrège tout en un nombre que personne
ne vit : neuf requêtes à une milliseconde et une à cent font une moyenne de dix millisecondes — une
latence qu'aucune des dix requêtes n'a connue. Le percentile, lui, désigne une requête réelle : le
p90 de cette distribution vaut une milliseconde — l'aberration se cache au-delà — et le p100 vaut
cent. Ce contraste est la leçon des queues de distribution : les valeurs aberrantes ne se voient
qu'aux percentiles qui les contiennent, et le choix du percentile d'un objectif est un choix de
**clientèle** — protéger le client médian, le centième, ou le millième, ce ne sont pas les mêmes
engagements ni les mêmes coûts.

**Pourquoi le rang plutôt que l'interpolation.** Les variantes interpolées — moyenner les deux
mesures qui encadrent le rang fractionnaire — produisent des valeurs plus « lisses » et strictement
fausses : une latence que personne n'a subie, invérifiable dans les journaux, introuvable dans les
traces. La méthode du rang rend toujours une mesure réelle : le p99 désigne une requête qu'on peut
retrouver, tracer, expliquer. Pour un chiffre d'objectif contractuel, cette traçabilité vaut plus que
la lissité.

**Pourquoi le plafond du rang, et pas l'arrondi.** La promesse d'un percentile est une couverture :
« au moins p pour cent des mesures sont sous cette valeur ». Le rang au plafond la garantit
mécaniquement ; l'arrondi au plus proche la casse sur les petits effectifs — le p25 de quatre mesures
arrondirait au rang un ou deux selon la convention, et la version basse couvrirait moins que le quart
promis. Sur les petits échantillons — précisément ceux des fenêtres d'alerte courtes — cette nuance
décide.

**Pourquoi la copie est un contrat.** Trier le tableau reçu en place « marcherait » — et corromprait
les données de l'appelant, qui réutilise souvent sa fenêtre de mesures pour d'autres calculs. La
non-mutation des entrées est vérifiée par la suite de cet exercice comme elle l'est par le vrai
runner : une fonction d'analyse qui modifie ses données d'entrée est un bogue d'intégration en
puissance, même quand son résultat est juste.

En entretien, les termes sont percentile, p99, tail latency — et la question type est exactement le
piège de l'énoncé : « votre moyenne est excellente, pourquoi vos clients se plaignent-ils ? ». La
réponse commence par la queue de distribution et se termine par le choix du percentile d'objectif.
