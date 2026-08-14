# Cartes de révision

## card-security-jwt-decode-001-rule

**Question :** Quelles restaurations le décodage Base64Url exige-t-il avant `Convert.FromBase64String` ?  
**Réponse attendue :** Remplacer le tiret et le souligné par le plus et la barre oblique, puis
compléter le remplissage selon la longueur modulo quatre — en refusant un reste de un, impossible
en Base64.

## card-security-jwt-decode-001-edge

**Question :** Pourquoi une revendication absente rend-elle la chaîne vide plutôt qu'une exception ?  
**Réponse attendue :** L'absence d'une revendication est une situation normale d'une charge utile,
pas une erreur de format ; l'exception reste réservée au jeton indécodable.
