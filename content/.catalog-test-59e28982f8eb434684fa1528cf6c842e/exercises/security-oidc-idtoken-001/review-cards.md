# Cartes de révision

## card-security-oidc-idtoken-001-rule

**Question :** À quel objet chacune des trois revendications propres du jeton d'identité
le lie-t-elle ?  
**Réponse attendue :** Le nonce à la demande du client, la partie autorisée au client lui-même,
l'empreinte d'accès au jeton d'accès émis dans la même réponse.

## card-security-oidc-idtoken-001-edge

**Question :** Comment se calcule la revendication d'empreinte du jeton d'accès ?  
**Réponse attendue :** Moitié gauche — seize octets — du condensat SHA-256 des octets ASCII du
jeton d'accès encodé, en Base64Url sans remplissage ; le condensat entier est le piège.
