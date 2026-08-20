# Cartes de révision

## card-front-cache-decision-001-rule

**Question :** Qu'apporte la fenêtre stale-revalidate entre les seuils de fraîcheur et d'expiration ?  
**Réponse attendue :** Elle sert immédiatement une réponse un peu ancienne tout en déclenchant une
revalidation en arrière-plan. L'utilisateur n'attend pas, et le cache se met à jour pour la fois
suivante ; c'est le compromis entre servir vite et servir juste.

## card-front-cache-decision-001-edge

**Question :** Que vaut le verdict pour un âge exactement égal à staleAfter, puis à expireAfter ?  
**Réponse attendue :** À staleAfter l'entrée bascule déjà en stale-revalidate, à expireAfter elle
bascule en expired : les deux bornes sont franchies dès l'égalité. Un âge négatif dû à une horloge
décalée reste frais.
