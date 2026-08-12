# Explication

Le travail réussit seulement si construction et tests réussissent tous les deux.

Une conjonction stricte, et rien d'autre : un travail annoncé réussi avec des tests rouges donne une assurance fausse, ce qui est pire qu'aucune assurance. L'équipe cesse alors de lire les résultats, et la chaîne devient décorative.

Les deux signaux ne sont pas non plus symétriques dans le temps : des tests exécutés sur une construction échouée n'ont rien vérifié. C'est pourquoi l'ordre des étapes compte autant que leur conjonction, chaque étape échouant vite et bloquant la suite. La décision est en temps constant.
