# Explication

Quand deux branches modifient les mêmes lignes, l'outil de fusion insère dans le fichier les
deux versions, encadrées de marqueurs — les chevrons d'ouverture, la ligne d'égals, les
chevrons de fermeture — et laisse l'humain trancher. Le détecteur de cet exercice cherche ces
marqueurs *résiduels* : ceux qu'une résolution bâclée a laissés dans le fichier commis.

L'énoncé demande ce qu'un marqueur oublié fait à la branche principale : dans un fichier
compilé, une erreur de syntaxe qui casse la construction pour toute l'équipe — c'est le cas
« bruyant », désagréable mais franc. Le cas dangereux est l'autre : dans un fichier *non
compilé* — configuration, données, documentation, script —, les marqueurs et les deux versions
concurrentes passent tels quels, et le système consomme un fichier qui contient littéralement
deux vérités superposées plus trois lignes de bruit. Une configuration avec marqueurs peut se
charger « avec succès » et produire un comportement indéfinissable. C'est pour ces fichiers-là
que la détection automatique existe — la compilation ne les protège pas.

La mécanique du détecteur porte deux choix. Chercher les trois marqueurs *séparément*, en
disjonction : une résolution à moitié faite — les chevrons du haut retirés, la ligne du milieu
oubliée — laisse un seul marqueur, et il suffit ; exiger le triplet complet raterait
précisément les résolutions bâclées, qui sont le cas d'usage. Et chercher par sous-chaîne
ordinale, sept caractères exacts : les marqueurs réels sont en début de ligne, et un détecteur
plus strict — ancré en début de ligne — éviterait le faux positif d'un texte qui *parle* des
marqueurs. Le contrat de l'exercice retient la version par sous-chaîne, plus simple et plus
sensible ; c'est un arbitrage sensibilité-contre-précision à connaître : pour un garde-fou de
pré-commit, le faux positif occasionnel se lève à la main, le faux négatif finit en
production.

La ligne d'égals mérite une note d'honnêteté : sept signes égal apparaissent parfois dans des
séparateurs décoratifs de commentaires — c'est le marqueur le plus sujet aux faux positifs, et
les détecteurs de production l'ancrent en début de ligne pour cette raison.

Les cas suivent l'énoncé : chaque marqueur détecté isolément, le texte propre, le vide.

Le coût est linéaire. La transposition est le principe du *garde-fou de pré-intégration* : les
erreurs mécaniques fréquentes — marqueurs, clés de débogage, fichiers temporaires — se
détectent par des vérifications automatiques en crochet local ou en intégration continue,
avant qu'elles n'atteignent la branche partagée. La revue humaine est trop précieuse pour
chasser des chevrons.
