# Cartes de révision

- **Qu'est-ce que l'identité attestée par la plateforme supprime que le coffre ne fait que
  déplacer ?** Le secret stocké lui-même : plus d'identifiant d'accès à protéger, donc plus de
  problème du premier secret — la régression des gardiens à garder s'interrompt.
- **Quand le coffre central est-il le bon canal pour un poste de développement ?** Pour une valeur
  qui tourne : le magasin utilisateur ne se resynchronise jamais et servirait la valeur d'hier ;
  seule une source centrale suit la rotation.
