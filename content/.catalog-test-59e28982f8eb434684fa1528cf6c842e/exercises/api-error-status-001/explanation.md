# Explication

Traduire une catégorie d'erreur interne en statut HTTP est une table de correspondance — et le
choix de son cas par défaut est une décision de *sécurité*, ce qui distingue cet exercice du
classement de statuts voisin.

Le principe est la liste blanche, et l'énoncé le nomme : seules les catégories *connues* —
validation, absence, conflit, authentification, autorisation — reçoivent leur statut précis ;
tout le reste tombe en 500, l'erreur interne générique. La question à se poser est celle que
l'énoncé pose : que divulguerait un statut trop précis ? Une catégorie interne inconnue du
contrat — « déni du pare-feu », « délai de la base » — traduite finement renseignerait un
attaquant sur l'architecture : quels composants existent, lesquels tombent, à quel rythme. Le
500 uniforme est volontairement pauvre : « quelque chose a échoué chez nous », rien de plus —
le détail appartient aux journaux internes, corrélés par identifiant, jamais à la réponse. La
liste blanche inverse la charge : une catégorie nouvelle est *muette par défaut*, et c'est une
décision explicite — une ligne ajoutée à la table — qui la rend éloquente. L'oubli est sûr ;
c'est exactement l'inverse d'une liste noire, où l'oubli publie.

La normalisation d'entrée mérite sa ligne : l'opérateur conditionnel nul absorbe l'absence —
`kind?.Trim()` rend `null` si `kind` l'est, et `null` ne correspond à aucun bras du `switch`
sauf au rejet par défaut — puis le rognage et la minuscule invariante font converger les
graphies. Une catégorie est un identifiant technique échangé entre couches internes ; la
tolérance de casse est un confort d'intégration, et l'invariant garantit le même verdict sur
toute machine. La chaîne d'appels compacte — présence, rognage, casse, table — est l'idiome à
retenir pour toute normalisation-puis-correspondance.

L'expression `switch` sur chaînes est la table rendue lisible : cinq entrées, un défaut, et le
compilateur exige ce défaut puisque le domaine des chaînes est infini — le pendant syntaxique
de la liste blanche.

Les cas cachés suivent les axes de l'énoncé : chaque catégorie connue vers son statut, la
casse mélangée qui converge, l'inconnue et l'absente qui tombent en 500 — le contrat de
sécurité fige que même une catégorie plausible mais non listée reste muette.

Le coût est constant. La transposition est la règle des frontières d'erreur : entre l'intérieur
bavard — exceptions typées, messages détaillés, piles — et l'extérieur public, une table de
traduction en liste blanche, un défaut pauvre, et les détails dans les journaux corrélés. Toute
API qui laisse ses exceptions internes choisir leurs statuts finit par raconter son
architecture à qui sait la questionner.
