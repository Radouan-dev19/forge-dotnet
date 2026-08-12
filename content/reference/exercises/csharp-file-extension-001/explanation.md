# Explication

Utiliser la fonction de chemin et retourner uniquement la dernière extension normalisée.

Un nom de fichier peut contenir plusieurs points : l'extension est le dernier segment, pas le second. Un découpage naïf sur le point se trompe dès qu'un nom porte une date ou un numéro de version, ce qu'un jeu d'essai à trois fichiers simples ne montre jamais. La fonction de chemin de la plateforme traite déjà ce cas, ainsi que le fichier sans extension et le nom qui commence par un point.

La normalisation de casse emploie la culture invariante pour la même raison qu'ailleurs : la conversion dépend de la culture pour certaines lettres, et une comparaison d'extension deviendrait dépendante de la machine. Le coût est linéaire dans la longueur du chemin.
