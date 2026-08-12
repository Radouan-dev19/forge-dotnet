# Explication

Refuser un identifiant non publiable puis construire un chemin relatif stable.

Un identifiant nul ou négatif ne désigne aucune ressource : composer une adresse à partir de lui produirait un lien mort, et l'appelant ne découvrirait le défaut qu'au moment de le suivre. Refuser à la source rend l'erreur immédiate et attribuable.

Le chemin est relatif à dessein. Une adresse absolue fige l'hôte dans la réponse, ce qui casse dès qu'un serveur mandataire, un nom de domaine différent ou un environnement de recette entrent en jeu. La barre de tête n'est pas décorative : sans elle, le chemin est interprété relativement au contexte courant, donc différemment selon le point d'entrée. La composition est en temps constant.
