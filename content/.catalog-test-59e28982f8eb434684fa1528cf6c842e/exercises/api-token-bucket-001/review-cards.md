# Cartes de révision

## card-api-token-bucket-001-rule

**Question :** Sur quoi s'applique le plafond dans un seau de jetons, et que borne-t-il ?  
**Réponse attendue :** Sur la recharge, pas sur le solde final : il borne la rafale — un seau
inactif se remplit jusqu'à la capacité et jamais au-delà.

## card-api-token-bucket-001-edge

**Question :** Que fait un seau vide face à un appel, et quel état est interdit ?  
**Réponse attendue :** Il refuse sans consommer, laissant le solde à zéro ; le solde négatif est
interdit, il fausserait la recharge suivante et l'admission.
