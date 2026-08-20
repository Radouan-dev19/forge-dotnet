# Cartes de révision

## card-api-cors-preflight-001-rule

**Question :** Que doit confirmer un préflight pour passer, et dans quel sens joue l'inclusion
des en-têtes ?  
**Réponse attendue :** La méthode demandée doit être autorisée ET chaque en-tête demandé doit
l'être ; l'inclusion va des demandés vers les autorisés, la liste autorisée pouvant être plus
large.

## card-api-cors-preflight-001-edge

**Question :** Une requête de préflight sans aucun en-tête demandé : refus ou passage ?  
**Réponse attendue :** Passage possible — une liste d'en-têtes demandés vide est une absence
d'exigence, pas un refus ; la décision se fait alors sur la seule méthode.
