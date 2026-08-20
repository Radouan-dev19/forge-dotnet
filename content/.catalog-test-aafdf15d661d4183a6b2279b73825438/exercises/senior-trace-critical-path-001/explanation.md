# Explication

La lecture naïve d'une trace lente accuse toujours le même innocent : le segment racine, qui « dure »
toute la requête. C'est l'artefact de la mesure — un segment englobe ses appels, donc les durées
remontent mécaniquement vers la racine — et l'équipe qui optimise sur les durées brutes passe ses
sprints à accélérer des segments qui attendent. Le temps propre corrige cette illusion, et sa
mécanique mérite d'être maîtrisée à la main avant d'être lue dans un outil.

**Pourquoi le temps propre, et pourquoi les enfants directs seulement.** La durée d'un segment se
décompose exactement en deux parts : ce que ses appels directs ont pris, et ce que son propre code a
fait — sérialisation, calcul, attente de verrou local. Soustraire les durées des enfants directs
isole cette seconde part. La tentation de soustraire toute la descendance est une erreur de double
compte : le temps des petits-enfants est déjà inclus dans celui des enfants, le retirer deux fois
produit des temps propres négatifs et un classement absurde. La récursion s'arrête au premier niveau
parce que la comptabilité y est déjà complète.

**Ce que le verdict change à l'enquête.** Dans l'exemple de l'énoncé, quatre segments se partagent la
requête : trois font trente millisecondes de travail propre, la base en fait cent dix. Le rapport
« la passerelle dure 200 ms » envoie l'équipe régler des délais d'attente ; le rapport « la base
travaille 110 ms en propre » l'envoie lire un plan de requête. Même trace, deux enquêtes — et seule
la seconde converge. C'est la version locale du chemin critique : sur une trace séquentielle, le
segment au temps propre maximal est l'endroit où une milliseconde gagnée est une milliseconde gagnée
pour la requête entière.

**Pourquoi le départage est du contrat.** Deux segments à temps propre égal sont fréquents — les
architectures symétriques y poussent. Sans règle, le verdict dépendrait de l'ordre du journal, et
deux exécutions du même rapport désigneraient deux coupables : le genre de non-déterminisme qui
décrédibilise un outil en une réunion. Le début le plus précoce puis l'ordre des noms sont
arbitraires mais stables, et la stabilité est ici la propriété qui compte.

**Pourquoi le parent inconnu se refuse.** Un lien cassé signifie que la soustraction attribuerait le
temps d'un enfant à personne : le temps propre du vrai parent serait gonflé d'autant, et le verdict
mentirait avec précision. Le diagnostic des liens cassés existe — c'est le comptage d'orphelins — et
il précède l'analyse de chemin ; les mélanger ferait de chaque trace abîmée un faux coupable.

En entretien, ce calcul se nomme self time, et il s'adosse au vocabulaire du traçage distribué —
span, trace, chemin critique. La question type est exactement le piège de l'énoncé : « le segment
racine dure toute la requête, que faites-vous ? » — et la réponse attendue commence par la
soustraction.
