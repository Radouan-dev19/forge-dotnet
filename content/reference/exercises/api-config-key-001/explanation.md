# Explication

Valider les deux segments et conserver le séparateur hiérarchique standard.

Une clé de configuration mal formée ne lève rien à la lecture : elle ne correspond simplement à aucune valeur, et le service démarre avec un repli silencieux. C'est exactement le défaut qu'une validation au démarrage doit rendre bruyant, et le refus à la composition en est la première ligne.

Les blancs de bordure sont la cause la plus fréquente : invisibles dans un fichier, ils produisent une clé voisine de celle attendue. Le séparateur, lui, n'est pas un choix esthétique — c'est celui que le système de configuration interprète comme un niveau de hiérarchie. Le coût est linéaire dans la longueur des segments.
