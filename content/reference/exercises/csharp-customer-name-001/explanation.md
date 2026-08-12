# Explication

Traiter absence et blanc avant toute déréférence ou conversion.

Trois entrées distinctes se ramènent au même repli : la valeur absente, la chaîne vide et la chaîne de blancs. Les traiter ensemble, en tête de méthode, évite la déréférence et couvre le cas le plus fréquent en production — un champ de formulaire contenant un espace.

L'ordre des deux opérations n'est pas indifférent : retirer les blancs puis convertir donne un nom propre, l'inverse les laisse en bordure. Et la culture invariante n'est pas un détail de style : la conversion en majuscules dépend de la culture pour certaines lettres, si bien que le même code produirait deux résultats selon la machine qui l'exécute. Le coût est linéaire dans la longueur du nom.
