# Cartes de révision

## card-dateonly-purpose-001

**Question :** Quand préférer `DateOnly` à `DateTime` ?  
**Réponse attendue :** Lorsque seule une date civile intervient et qu’aucune heure ni zone n’a de sens métier.

## card-business-boundary-001

**Question :** Quel test prouve que la borne finale est incluse ?  
**Réponse attendue :** Une plage d’un seul lundi doit retourner 1 et une plage d’un seul samedi 0.
