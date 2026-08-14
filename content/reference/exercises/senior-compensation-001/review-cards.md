# Cartes de révision

## card-senior-compensation-001-rule

**Question :** Dans quel ordre execute-t-on les actions compensatoires d'une saga, et pourquoi ?  
**Réponse attendue :** Dans l'ordre strictement inverse des etapes reussies : la derniere posee est la premiere annulee, car les etapes tardives dependent des precedentes et les defaire d'abord respecte ces dependances.

## card-senior-compensation-001-edge

**Question :** Que rend la fonction pour une saga dont aucune etape n'a reussi ?  
**Réponse attendue :** Une chaine vide : il n'y a aucun effet a defaire, et fabriquer une action compensatoire serait un effet de bord invente.
