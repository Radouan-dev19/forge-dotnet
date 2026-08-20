# Cartes de révision

## card-string-token-001

**Question :** Comment définir un mot sans énumérer toute la ponctuation ?  
**Réponse attendue :** Définir les caractères autorisés, ici `char.IsLetterOrDigit`, et traiter tout le reste comme séparateur.

## card-string-finalize-001

**Question :** Pourquoi finaliser le mot une seconde fois après la boucle ?  
**Réponse attendue :** Pour traiter un texte dont le dernier caractère appartient au dernier mot.
