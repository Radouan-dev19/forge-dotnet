# Explication

Valider trois entiers non négatifs et incrémenter seulement le composant de correction.

Une correction n'ajoute rien et ne casse rien : seul le troisième composant bouge, et les deux autres restent inchangés. Les remettre à zéro serait le comportement d'une incrémentation de rang supérieur, où les composants de droite repartent effectivement de zéro.

La validation préalable évite de produire un numéro qui ne désigne rien. Le critère qui gouverne le choix du composant est le même partout : un appelant écrit avant ce changement continue-t-il de fonctionner sans être modifié ? Oui sans rien de plus, c'est une correction. Le coût est linéaire dans la longueur de la version.
