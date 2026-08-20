# Cartes de révision

## card-security-jwt-order-001-rule

**Question :** Où passe la ligne de partage qui fonde l'ordre des contrôles d'un validateur de
jeton ?  
**Réponse attendue :** Entre ce qui s'établit sans faire confiance au contenu — forme, algorithme,
signature — et les revendications, qui n'ont de valeur qu'une fois la signature prouvée.

## card-security-jwt-order-001-edge

**Question :** Quel verdict rend un validateur correct sur un jeton à la fois expiré et falsifié ?  
**Réponse attendue :** Le verdict de signature : l'expiration lue dans une charge utile non
authentifiée est une donnée de l'attaquant, pas un fait.
