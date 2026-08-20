# Explication

Le palindrome est un classique d'entretien parce qu'il condense trois discussions en dix
lignes : ce qu'on normalise, comment on compare, et quand on s'arrête.

La normalisation d'abord, parce qu'elle définit le problème avant que l'algorithme n'existe.
« Ésope reste ici et se repose » n'est un palindrome qu'à condition d'ignorer espaces et casse —
la solution retire les espaces et aplanit en minuscules invariantes, *parce que l'énoncé
l'annonce*. C'est le point de méthode : la normalisation n'est pas un embellissement du code,
c'est une clause du contrat, et deux implémentations qui normalisent différemment répondent à
deux questions différentes. La ponctuation, elle, n'est pas retirée ici — le contrat ne le
promet pas — et un candidat qui l'ignore « pour bien faire » a changé de sujet sans le dire.

La comparaison ensuite, par deux index qui convergent — le motif *two pointers* dans sa forme
native. `left` part du début, `right` de la fin ; chaque tour compare une paire symétrique puis
resserre l'étau. La borne `left < right` mérite l'attention : à l'égalité — le caractère central
d'une chaîne impaire — il n'y a personne en face, la comparaison est inutile et la boucle
s'arrête. Écrire `<=` ferait comparer le centre à lui-même, toujours vrai, sans changer le
verdict : le choix strict n'est pas une correction mais une précision — ne faire que le travail
qui décide. Les longueurs paires et impaires passent par le même code, et c'est le signe d'une
borne bien choisie.

L'arrêt enfin : le premier désaccord suffit, `return false` immédiat. Poursuivre pour « finir la
boucle » serait du travail mort — la moitié des chaînes réelles divergent dès les premières
paires, et la sortie précoce fait de la longueur un *pire cas*, pas un coût systématique. La
chaîne vide et le caractère unique rendent vrai par non-exécution de la boucle : des palindromes
par vacuité, cohérents avec la définition, sans garde spéciale.

Une réserve d'honnêteté : l'indexation par `char` travaille sur des unités UTF-16, pas des
graphèmes — les accents combinés ou les émojis brisent la symétrie apparente. Pour le domaine de
l'exercice, lettres simples et espaces, c'est exact ; pour du texte arbitraire, il faudrait
raisonner en éléments de texte, et savoir *que* cette limite existe vaut mieux que de la
découvrir.

Le coût : une allocation pour la normalisation, puis une passe à deux index — linéaire, sortie
précoce comprise. La transposition du motif dépasse les palindromes : recherche de paire dans un
tableau trié, fusion par les extrémités, partitionnement — deux index qui convergent sont l'un
des squelettes les plus réutilisés de l'algorithmique de tableaux.
