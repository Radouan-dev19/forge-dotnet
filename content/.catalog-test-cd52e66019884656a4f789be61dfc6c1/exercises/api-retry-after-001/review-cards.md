# Cartes de révision

## card-api-retry-after-001-rule

**Question :** Jusqu'à quel instant précis court le Retry-After d'une fenêtre fixe, et que
vaut-il sous le quota ?  
**Réponse attendue :** Jusqu'au début de la fenêtre suivante — début de la fenêtre courante plus
sa durée ; sous le quota, zéro, le client n'a rien à attendre.

## card-api-retry-after-001-edge

**Question :** Que rendre si l'instant courant a déjà dépassé la réinitialisation de la fenêtre ?  
**Réponse attendue :** Zéro : le délai borné par le bas signifie « réessaie maintenant », un
Retry-After négatif n'ayant aucun sens.
