# Cartes de révision

## card-api-version-change-001-rule

**Question :** Quelle règle range un changement d'API du côté sûr ou cassant ?  
**Réponse attendue :** L'asymétrie ajouter/retirer/restreindre — ajouter du facultatif laisse les
appels existants intacts, retirer ou restreindre les casse.

## card-api-version-change-001-edge

**Question :** De quel côté tombe une étiquette de changement inconnue, et pourquoi ?  
**Réponse attendue :** Du côté cassant, par présomption de danger : présumer compatible
laisserait une régression filer en production chez des consommateurs non contrôlés.
