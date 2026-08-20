# Cartes de révision

## card-price-decimal-001

**Question :** Pourquoi préférer `decimal` à `double` pour un prix ?  
**Réponse attendue :** Pour représenter précisément les fractions décimales usuelles et maîtriser l’arrondi financier.

## card-price-rounding-001

**Question :** Dans quel ordre convertir 12,34 € en centimes ?  
**Réponse attendue :** Valider, multiplier le `decimal` par 100, arrondir explicitement, puis convertir en entier.
