# Cartes de révision

## card-front-form-field-state-001-rule

**Question :** Pourquoi séparer l'état touché de l'état valide d'un champ de formulaire ?  
**Réponse attendue :** Parce qu'un champ peut être invalide dès l'ouverture sans qu'il faille encore
le signaler ; l'interface attend le premier contact pour afficher l'erreur. Garder les deux
dimensions distinctes laisse la couche de rendu choisir quand parler.

## card-front-form-field-state-001-edge

**Question :** Que doit remettre à zéro un reset, au-delà de la valeur saisie ?  
**Réponse attendue :** Aussi la marque de contact : le champ repasse non touché en même temps que sa
valeur redevient vide. Sinon un champ vidé continue d'afficher son erreur comme s'il restait touché.
