# Cartes de révision

## card-security-pkce-challenge-001-rule

**Question :** Quels trois gestes ordonnés produisent l'empreinte S256 d'un secret PKCE ?  
**Réponse attendue :** Condensat SHA-256 des octets ASCII du secret, encodage Base64, puis
traduction vers l'alphabet urlisé sans remplissage — tiret, souligné, égals retirés.

## card-security-pkce-challenge-001-edge

**Question :** Pourquoi un secret PKCE de moins de 43 caractères est-il refusé d'office ?  
**Réponse attendue :** La borne basse garantit l'entropie minimale de la preuve : plus court,
le secret devient énumérable et le code intercepté redevient exploitable.
