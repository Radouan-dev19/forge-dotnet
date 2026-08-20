# Cartes de révision

## card-security-jwt-audience-001-rule

**Question :** Quelles formes JSON la revendication d'audience peut-elle légitimement prendre ?  
**Réponse attendue :** Une chaîne unique ou un tableau de chaînes ; toute autre forme se refuse,
et un vérificateur doit gérer les deux formes valides.

## card-security-jwt-audience-001-edge

**Question :** Quelle attaque le contrôle d'audience bloque-t-il que la signature laisse passer ?  
**Réponse attendue :** Le rejeu croisé : un jeton authentique émis pour un service présenté à un
autre service du même émetteur, qui partage la clé de vérification.
