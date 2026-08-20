# Cartes de révision

## card-api-webhook-replay-001-rule

**Question :** Que protège la fenêtre d'horodatage d'un webhook que sa signature ne protège pas ?  
**Réponse attendue :** Le rejeu — un envoi authentique capturé et renvoyé reste valide sans elle ;
la signature dit qui et garantit l'intégrité, mais pas la fraîcheur.

## card-api-webhook-replay-001-edge

**Question :** Pourquoi la fenêtre est-elle symétrique autour de l'instant courant ?  
**Réponse attendue :** L'horloge de l'émetteur peut être légèrement en avance ; un envoi dont
l'horodatage est dans un futur proche est aussi légitime qu'un envoi en léger retard, d'où
l'écart absolu.
