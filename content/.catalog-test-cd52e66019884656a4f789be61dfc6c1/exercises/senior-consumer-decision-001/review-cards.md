# Cartes de revision

## card-senior-consumer-decision-001-rule

**Question :** Dans quel ordre teste-t-on les regles d'un consommateur de messages ?  
**Reponse attendue :** Validation du compteur de livraison, puis mise a l'ecart pour livraisons
excessives, puis detection de doublon, puis traitement nominal ; la premiere regle applicable decide.

## card-senior-consumer-decision-001-edge

**Question :** Pourquoi la mise a l'ecart doit-elle passer avant la detection de doublon ?  
**Reponse attendue :** Un message empoisonne aussi present parmi les traites serait sinon rejoue en
boucle ; l'ecarter d'abord isole le message defaillant et laisse la file avancer.
