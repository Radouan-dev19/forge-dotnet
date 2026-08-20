# Cartes de révision

## card-api-etag-precondition-001-rule

**Question :** Quel en-tête gouverne la lecture conditionnelle, lequel gouverne l'écriture, et
quels statuts chacun rend ?  
**Réponse attendue :** If-None-Match gouverne la lecture — 200 ou 304 ; If-Match gouverne
l'écriture — 200 si l'état n'a pas bougé, 412 sinon, 428 si aucune condition n'est fournie.

## card-api-etag-precondition-001-edge

**Question :** Pourquoi refuser par 428 une écriture qui ne porte aucun If-Match ?  
**Réponse attendue :** Sans condition, l'écriture s'applique à l'aveugle et peut écraser une
modification concurrente — la mise à jour perdue ; le 428 exige que le client pose sa condition.
