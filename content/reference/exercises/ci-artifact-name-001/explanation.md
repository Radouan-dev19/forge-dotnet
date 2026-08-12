# Explication

Normaliser la branche, remplacer le séparateur et exiger un numéro positif.

Le remplacement du séparateur n'est pas cosmétique : une branche nommée avec une barre oblique produirait un nom interprété comme un chemin, et le stockage de l'artefact casserait. C'est une condition de fonctionnement, pas une préférence de lisibilité.

Le nom a une fonction précise : permettre, devant un incident, de remonter du fichier déployé au commit exact. C'est pourquoi il porte la branche et le numéro d'exécution, et pourquoi un numéro invalide est refusé plutôt qu'absorbé — un artefact non identifiable transforme un diagnostic en enquête. Le coût est linéaire dans la longueur du nom de branche.
