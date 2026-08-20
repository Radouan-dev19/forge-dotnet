# Cartes de révision

## card-api-etag-compute-001-rule

**Question :** Quelles deux propriétés un ETag doit-il avoir, et pourquoi un condensat les
donne-t-il ?  
**Réponse attendue :** Stable — même représentation, même empreinte — et sensible — le moindre
changement la change ; le SHA-256 garantit les deux sur une suite d'octets donnée.

## card-api-etag-compute-001-edge

**Question :** Pourquoi fixer la casse de l'hexadécimal dans l'empreinte ?  
**Réponse attendue :** Majuscules et minuscules encodent le même condensat mais donnent deux
textes différents ; sans casse fixée, deux calculs ne coïncideraient pas et l'ETag serait
instable.
