# Explication

Exiger un préfixe réservé et un suffixe suffisamment unique, avec comparaison ordinale.

Le préfixe a deux fonctions, et la seconde est la plus importante : il rend le nettoyage sûr. Un script qui supprime les bases préfixées ne pourra jamais supprimer une base réelle par accident. C'est une garantie de forme, pas de fond, mais elle empêche la catastrophe la plus banale d'un environnement de test.

La longueur du suffixe conditionne le parallélisme : trop court, deux exécutions simultanées peuvent tomber sur le même nom et se marcher dessus, ce qui produit des échecs intermittents impossibles à reproduire. La comparaison est ordinale parce qu'un nom de base est une valeur technique. Le coût est linéaire dans la longueur du nom.
