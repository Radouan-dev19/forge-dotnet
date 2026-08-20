# Cartes de révision

## card-security-jwt-lifetime-001-rule

**Question :** Pourquoi l'absence d'expiration refuse-t-elle le jeton alors que l'absence de prise
d'effet le laisse passer ?  
**Réponse attendue :** Un jeton sans expiration ne meurt jamais et un jeton volé devient un accès
permanent ; l'absence de prise d'effet signifie seulement valable dès l'émission.

## card-security-jwt-lifetime-001-edge

**Question :** Dans quel sens la tolérance d'horloge s'applique-t-elle à chacune des deux bornes ?  
**Réponse attendue :** Elle élargit la fenêtre : l'expiration est repoussée de la tolérance, la
prise d'effet avancée d'autant — jamais l'inverse.
