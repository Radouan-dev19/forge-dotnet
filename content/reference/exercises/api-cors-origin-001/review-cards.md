# Cartes de révision

## card-api-cors-origin-001-rule

**Question :** Dans quel unique cas le joker d'origine est-il légitime ?  
**Réponse attendue :** Ressource ouverte à tous ET requête sans identifiants ; dès que des
identifiants sont en jeu, la spécification interdit le joker et il faut une origine nommée.

## card-api-cors-origin-001-edge

**Question :** Pourquoi l'écho de l'origine reçue sans liste blanche est-il dangereux ?  
**Réponse attendue :** Il revient à autoriser toutes les origines, identifiants compris —
le serveur dit « oui, toi » à chaque site —, contournant le verrou joker/identifiants ; seul
l'écho confronté à une liste fermée est sûr.
