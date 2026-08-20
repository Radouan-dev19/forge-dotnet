# Cartes de révision

## card-security-jwt-signature-001-rule

**Question :** Sur quelle chaîne exacte le condensat HMAC d'un jeton est-il calculé ?  
**Réponse attendue :** Sur les deux premiers segments encodés joints par le point séparateur —
jamais sur la charge utile décodée ni sur la charge utile seule.

## card-security-jwt-signature-001-edge

**Question :** Que répond le vérificateur à un jeton dont la signature n'est pas décodable ?  
**Réponse attendue :** Faux, sans exception : un jeton malformé est le quotidien d'un
vérificateur, et une exception offrirait un canal de sonde.
