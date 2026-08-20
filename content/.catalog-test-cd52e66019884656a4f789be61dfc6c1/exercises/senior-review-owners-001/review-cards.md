# Cartes de révision

- **Pourquoi la résolution des propriétaires prend-elle le préfixe le plus long ?** Parce que la
  propriété la plus spécifique connaît le mieux le code : convoquer toute la chaîne imbriquée double
  les coûts et dilue la responsabilité — l'englobant reste le repli.
- **Que fait la politique d'un fichier qu'aucun préfixe de propriété ne couvre ?** Elle refuse : la
  zone grise est le trou du mécanisme — du code sans propriétaire est du code sans relecteur, et la
  carte se met à jour au moment où le trou apparaît.
