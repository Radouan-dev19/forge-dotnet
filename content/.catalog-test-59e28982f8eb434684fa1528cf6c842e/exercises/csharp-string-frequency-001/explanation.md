# Explication

Compter les mots d'un texte est le premier vrai exercice de *tokenisation* du catalogue : la
définition du mot est donnée — une suite non vide de caractères lettre-ou-chiffre — et tout le
travail consiste à la transcrire sans l'abîmer.

Le réflexe `Split` est ici le mauvais outil, et comprendre pourquoi vaut l'exercice. `Split`
exige la liste des séparateurs ; or le contrat définit l'inverse — ce qui *compose* un mot — et
tout le reste sépare. Énumérer « tout le reste » est impossible : ponctuation, symboles,
retours à la ligne, caractères exotiques. La solution renverse donc la logique : un automate
minuscule parcourt le texte caractère par caractère, accumule dans un `StringBuilder` tant que
`char.IsLetterOrDigit` répond vrai, et clôt le mot courant dès qu'un autre caractère surgit.
Définir les tokens par leur alphabet plutôt que par leurs séparateurs : c'est la leçon qui se
transpose à tous les découpages non triviaux.

La clôture du mot est extraite dans une fonction locale `CompleteWord`, et sa première ligne —
ne rien faire si l'accumulateur est vide — absorbe silencieusement les séparateurs consécutifs :
deux virgules de suite ne fabriquent pas de mot fantôme. L'appel final après la boucle traite le
piège que l'énoncé souligne : un texte qui se termine sans séparateur laisserait son dernier mot
dans l'accumulateur, jamais compté. Ce « vidage final » est le hors-par-un des automates à
accumulation, cousin de celui des boucles d'indices, et le cas caché au texte sans ponctuation
finale le vérifie.

Deux normalisations fixent l'identité des mots. La casse : chaque caractère passe par
`ToLowerInvariant` à l'accumulation, si bien que deux graphies du même mot fusionnent — et
l'invariant garantit le même verdict sur toutes les machines. Le comparateur du dictionnaire :
`StringComparer.Ordinal`, cohérent avec des clés déjà normalisées — comparer binairement des
minuscules invariantes est exact et rapide. Le cumul, lui, reprend le motif
`TryGetValue`-puis-réécriture : lire le compte courant, défaut zéro, écrire le compte plus un.

Les bornes découlent des définitions : la chaîne vide ou blanche rend un dictionnaire vide —
aucun caractère de mot, aucun mot — et `null` reste une faute d'appel. Les chiffres sont des
caractères de mot à part entière : un identifiant comme `abc123` est un seul mot, pas deux.

Le coût est linéaire en la longueur du texte, avec un dictionnaire proportionnel au vocabulaire.
La transposition est directe : nuages de mots, index de recherche, détection de doublons de
libellés — toute analyse de texte commence par cette même question « qu'est-ce qu'un token ? »,
et par un automate qui n'oublie ni les séparateurs répétés, ni le dernier mot.
