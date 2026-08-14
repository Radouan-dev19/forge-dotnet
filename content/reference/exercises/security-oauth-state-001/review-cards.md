# Cartes de révision

## card-security-oauth-state-001-rule

**Question :** Dans quel ordre les registres se consultent-ils pour classer un state de retour ?  
**Réponse attendue :** L'absence d'abord, puis le registre des consommés — le rejeu prime —,
puis les attentes ; ce qui ne figure nulle part est forgé.

## card-security-oauth-state-001-edge

**Question :** Que signale un state de retour que le client n'a jamais émis ?  
**Réponse attendue :** Une requête forgée inter-site en cours : quelqu'un fabrique des retours
de redirection — verdict distinct du rejeu, à journaliser comme tel.
