# Cartes de révision

## card-security-refresh-rotation-001-rule

**Question :** Quelles deux horloges bornent la durée de chaque jeton de rafraîchissement
tourné ?  
**Réponse attendue :** La durée de glissement — fenêtre normale entre rotations — et l'échéance
absolue de la session ; le jeton reçoit le minimum des deux, et les dernières fenêtres
rétrécissent.

## card-security-refresh-rotation-001-edge

**Question :** Pourquoi une session sans échéance absolue est-elle un défaut de sécurité ?  
**Réponse attendue :** Le glissement seul rend la session immortelle tant qu'elle est utilisée :
un jeton volé activement exploité se renouvelle sans fin.
