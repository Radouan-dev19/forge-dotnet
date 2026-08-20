# Explication

Classer le risque d'un changement pour doser l'attention de revue : la fonction croise un
volume et un indicateur de nature, et sa hiérarchie interne est la leçon — le *quoi* prime sur
le *combien*.

L'énoncé demande ce qu'un classement par volume seul laisserait passer : le diff de trois
lignes qui touche l'autorisation. Trois lignes, risque « faible » au volume — et ces trois
lignes ouvrent peut-être une route à tous les utilisateurs authentifiés. Les incidents de
sécurité naissent rarement des grandes refontes, surveillées par nature ; ils naissent des
petites retouches « évidentes » sur du code sensible, relues en diagonale précisément parce
qu'elles sont petites. D'où la règle : toucher l'autorisation classe *haut*, quel que soit le
volume — la disjonction `touchesAuthorization || changedLines > 300` met la nature et
l'extrême volume dans le même verdict, et l'ordre des gardes rend la priorité lisible. Le cas
caché du petit diff d'autorisation est le verrou de cette hiérarchie.

Le volume affine les cas restants, par paliers exclusifs : au-delà de trois cents lignes, haut
— la relecture attentive d'un tel diff dépasse ce qu'un cerveau tient en une session, et le
classement le dit ; au-delà de quatre-vingts, moyen ; en dessous, bas. Les seuils sont des
politiques d'équipe — les valeurs exactes se calibrent — mais leurs frontières sont du
contrat : trois cents lignes pile est *moyen* — la comparaison est strictement supérieure —
et quatre-vingts pile est *bas*. Les cas posés sur ces deux paliers départagent les
implémentations au caractère près, la mécanique de frontière habituelle.

Le volume négatif lève : une mesure de diff négative est un bug de l'outillage amont — l'outil
qui compte les lignes a déraillé — et le signaler vaut mieux que de classer l'absurde. On
retrouve la distinction posée par le budget d'imbrication voisin : les prédicats de politique
valident leurs mesures avant de les juger.

Trois verdicts textuels en sortie — pas un score numérique : le classement est fait pour
déclencher des *règles* — bloquer, exiger deux relecteurs, notifier — et trois catégories
nommées se branchent mieux qu'un continuum à interpréter.

Le coût est constant. La transposition est le tri de l'attention, partout où elle est rare :
priorisation des alertes, des tickets, des dettes — toujours croiser un axe de *gravité de
nature* et un axe de *volume*, et toujours laisser la nature primer. Le classement inverse —
le volume d'abord — est l'erreur des systèmes qui noient le signal critique dans le tout-venant
volumineux ; cette petite fonction est le contre-exemple à garder en tête.
