# Cartes de révision

## card-api-cache-storability-001-rule

**Question :** Quelles deux conditions cumulatives rendent une réponse stockable en cache ?  
**Réponse attendue :** Une méthode de lecture sans effet — GET ou HEAD — et un statut figurant
dans la liste fermée des statuts stockables ; l'une sans l'autre ne suffit pas.

## card-api-cache-storability-001-edge

**Question :** Pourquoi la réponse d'un POST qui répond 200 n'est-elle pas stockable ?  
**Réponse attendue :** Resservir la réponse d'une action laisserait croire que l'action a eu
lieu alors qu'aucune requête n'est partie ; seules les lectures sans effet sont stockables.
