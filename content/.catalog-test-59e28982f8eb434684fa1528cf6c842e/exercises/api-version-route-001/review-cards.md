# Cartes de révision

## card-api-version-route-001-rule

**Question :** Quelles trois issues distingue la résolution de version d'une route ?  
**Réponse attendue :** Aucune version demandée — on sert le défaut ; version demandée et prise en
charge — on la sert ; version demandée mais inconnue — verdict d'inexistence, jamais un repli.

## card-api-version-route-001-edge

**Question :** Pourquoi ne pas rabattre une version inconnue sur la version par défaut ?  
**Réponse attendue :** Le client croirait parler à la version demandée et recevrait celle du
défaut ; le décalage produit des comportements inexplicables — le repli masque une erreur
explicite.
