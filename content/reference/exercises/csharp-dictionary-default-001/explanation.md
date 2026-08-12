# Explication

Distinguer une clé absente d'une exception et ne jamais muter le dictionnaire.

Deux absences ne se valent pas. Un dictionnaire absent est une faute d'appelant et doit lever ; une clé absente est un cas normal et vaut zéro, ce qui évite à chaque appelant de tester la présence avant de lire. Indexer directement confondrait les deux en levant dans le second cas.

La lecture par tentative n'insère rien, et c'est ce qui compte : la variante qui écrit une valeur par défaut pour tester la présence fait grossir le dictionnaire à chaque consultation, défaut qui ne se manifeste qu'en charge. La lecture reste en temps constant amorti.
