# Cartes de révision

## card-front-state-reducer-001-rule

**Question :** Pourquoi un réducteur doit-il construire un nouvel état plutôt que modifier celui qu'il reçoit ?  
**Réponse attendue :** Parce que d'autres vues peuvent détenir une référence vers l'ancien état ;
le muter les ferait changer sans intention explicite. Un état neuf reste figé, comparable et
testable par simple confrontation entrée-sortie.

## card-front-state-reducer-001-edge

**Question :** Que doit faire un incrément demandé sur une valeur qui n'est pas un entier valide ?  
**Réponse attendue :** Abandonner l'action et laisser la valeur intacte, jamais écrire un zéro ni
lever une erreur. On évite ainsi de transformer silencieusement du texte en nombre et de perdre la
donnée d'origine.
