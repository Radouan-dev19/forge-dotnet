# Explication

Convertir les clés avec la culture invariante et incrémenter une seule entrée.

Une clé produite par conversion de texte dépend de la culture : le séparateur, le signe, parfois les chiffres eux-mêmes changent. Une clé qui varie avec la machine rend le dictionnaire non reproductible, et le défaut n'apparaît qu'au déploiement sur une machine configurée autrement. La culture invariante supprime cette dépendance.

La lecture par tentative évite l'exception sur une clé encore absente, et l'écriture unique évite d'accumuler des entrées. Le comparateur ordinal est cohérent avec des clés numériques, où aucune équivalence culturelle n'a de sens. Le parcours est linéaire et l'espace croît avec le nombre de valeurs distinctes.
