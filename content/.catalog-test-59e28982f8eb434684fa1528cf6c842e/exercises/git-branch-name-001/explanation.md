# Explication

Une normalisation de nom paraît décorative. Elle règle en réalité un problème d'équipe très concret :
sur un système de fichiers sensible à la casse — Linux, donc la plupart des serveurs
d'intégration — `Feature/Import` et `feature/import` sont **deux branches distinctes**. Sur macOS ou
Windows, ce sont souvent la même. Une équipe mixte finit par avoir deux branches qui portent le même
travail, dont une seule est fusionnée, et personne ne comprend pourquoi le correctif a disparu.

**Convertir, jamais supprimer.** C'est la décision centrale, et l'erreur la plus fréquente est de
faire l'inverse. Supprimer les caractères interdits transforme `import csv` en `importcsv` : deux
mots collés, illisibles à la relecture, et surtout impossibles à distinguer de `importc sv` ou de
tout autre découpage. Les convertir en séparateur conserve la frontière entre les mots, qui est
précisément l'information que l'espace portait.

**Le tiret est traité comme un séparateur, pas comme une lettre conservée.** Ce détail est ce qui
fait tenir la réduction. Si le tiret était simplement retenu, une suite `--` écrite à la main
survivrait, et `fix--total` cohabiterait avec `fix-total` : deux écritures pour une intention, soit
exactement ce que la normalisation devait supprimer. En le faisant passer par la branche des
séparateurs, la règle « un séparateur consécutif n'écrit rien » s'applique uniformément, quelle que
soit l'origine du caractère.

**La barre oblique survit parce qu'elle porte du sens.** Elle sépare les espaces de noms — `feature/`,
`fix/`, `chore/` — et les outils s'en servent pour regrouper. La convertir en tiret aplatirait cette
hiérarchie et ferait perdre le classement que l'équipe a choisi.

**Le refus est nécessaire, et l'alternative est pire.** Un nom qui ne laisse rien après normalisation
pourrait rendre une chaîne vide. L'appelant créerait alors une branche sans nom, ou plus
vraisemblablement l'outil échouerait plus loin, avec un message qui ne parle plus du nom d'origine.
Refuser au moment où l'information est encore là — le nom brut, le paramètre fautif — donne un
message que l'on peut agir.

**La normalisation est idempotente**, et c'est une propriété à vérifier plutôt qu'à espérer :
appliquée deux fois, elle rend le même résultat. Sans cette propriété, une chaîne d'outils qui
normalise à plusieurs étapes produirait des noms différents selon le nombre de passages — un défaut
qui ne se manifeste qu'en production, dans le pipeline le plus long.

Le coût est linéaire en longueur du nom, avec un seul parcours et un tampon de taille comparable.
C'est la borne évidente pour une transformation caractère par caractère, et il n'y a aucune raison de
chercher mieux : les noms de branche font quelques dizaines de caractères.
