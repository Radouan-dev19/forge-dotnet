# Cartes de révision

## card-senior-legacy-trace-001-rule

**Question :** Que fait exactement le mot void dans ce grand livre hérité ?  
**Réponse attendue :** Il annule l'effet de la derniere entree appliquee en le retranchant du solde ; ce n'est pas une remise a zero, et il faut empiler l'effet signe pour annuler un debit dans le bon sens.

## card-senior-legacy-trace-001-edge

**Question :** Que doit produire un void qui ne suit aucune entree appliquee ?  
**Réponse attendue :** Aucun effet : il n'y a rien a annuler, et le faire echouer serait plus strict que le comportement observe du code hérité.
