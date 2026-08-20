# Cartes de revision

## card-senior-circuit-breaker-001-rule

**Question :** Que fait le disjoncteur d'un echec recu dans l'etat half-open ?  
**Reponse attendue :** Il rouvre le circuit immediatement en repassant a open, sans attendre le
seuil d'echecs consecutifs : l'essai probatoire a echoue, le service est repute encore casse.

## card-senior-circuit-breaker-001-edge

**Question :** Pourquoi l'etat open ignore-t-il les jetons ok et fail ?  
**Reponse attendue :** En open aucun appel reel n'est emis, donc il n'y a rien a observer ; seul
l'ecoulement du temps, le jeton tick, fait passer a half-open pour tenter un unique essai.
