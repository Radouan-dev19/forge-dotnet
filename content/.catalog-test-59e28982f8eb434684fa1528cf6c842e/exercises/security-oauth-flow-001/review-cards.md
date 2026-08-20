# Cartes de révision

## card-security-oauth-flow-001-rule

**Question :** Quelles deux questions, dans quel ordre, choisissent un flux OAuth ?  
**Réponse attendue :** Un humain est-il présent — alors code d'autorisation avec preuve
d'échange, toujours — puis le client garde-t-il un secret — identifiants client si oui, refus si
non.

## card-security-oauth-flow-001-edge

**Question :** Que rend un profil de client contradictoire ou incomplet, et pourquoi pas un
défaut ?  
**Réponse attendue :** Le verdict d'invalidité : deviner un axe manquant choisirait un flux de
sécurité sur une hypothèse, et arbitrer une contradiction masquerait l'erreur amont.
