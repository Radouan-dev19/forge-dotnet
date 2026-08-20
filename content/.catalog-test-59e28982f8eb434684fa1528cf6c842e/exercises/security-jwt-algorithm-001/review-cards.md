# Cartes de révision

## card-security-jwt-algorithm-001-rule

**Question :** Pourquoi la casse se traite-t-elle différemment entre le refus de none et la
confrontation à l'algorithme exigé ?  
**Réponse attendue :** Le refus de none est une liste noire, donc insensible à la casse pour ne
rien laisser passer ; la confrontation à l'exigence suit la norme JOSE, stricte et sensible à la
casse.

## card-security-jwt-algorithm-001-edge

**Question :** Que gagne un attaquant si le vérificateur choisit son mécanisme d'après l'en-tête ?  
**Réponse attendue :** Il choisit le tribunal : jeton nu accepté via none, ou HMAC forgé avec la
clé publique du serveur par confusion d'algorithmes.
