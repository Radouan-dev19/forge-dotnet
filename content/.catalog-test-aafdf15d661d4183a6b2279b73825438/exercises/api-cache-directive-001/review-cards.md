# Cartes de révision

## card-api-cache-directive-001-rule

**Question :** Pourquoi la sensibilité se teste-t-elle en premier dans la composition d'une
directive de cache ?  
**Réponse attendue :** Parce qu'une donnée sensible marquée publique par erreur est la pire
issue — un cache partagé l'exposerait ; tester la sensibilité d'abord rend cette erreur
impossible.

## card-api-cache-directive-001-edge

**Question :** Que rend une nature de réponse inconnue, et selon quel principe ?  
**Réponse attendue :** no-store, par présomption de prudence : une réponse qu'on ne sait pas
classer ne se met pas en cache, un défaut permissif l'exposerait.
