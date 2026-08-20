# Cartes de revision

## card-senior-idempotency-key-001-rule

**Question :** Comment une cle d'idempotence distingue-t-elle un traitement d'un rejeu ?  
**Reponse attendue :** La premiere apparition d'une cle est un traitement veritable, on l'ajoute a
la memoire ; toute apparition ulterieure est un rejeu dont la reponse est deja connue.

## card-senior-idempotency-key-001-edge

**Question :** Pourquoi faut-il tester la presence d'une cle avant de l'inserer, jamais l'inverse ?  
**Reponse attendue :** Inserer avant de tester rendrait l'ensemble toujours porteur de la cle au
moment du test, et la premiere apparition serait declaree rejeu a tort.
