# Cartes de révision

## card-front-route-guard-001-rule

**Question :** Quelles sont les trois issues d'un garde de route, et à quel profil d'utilisateur correspond chacune ?  
**Réponse attendue :** Laisser passer un utilisateur authentifié qui a le droit exigé, interdire un
utilisateur authentifié à qui ce droit manque, rediriger vers la connexion un utilisateur non
reconnu. Confondre les deux dernières renvoie à la connexion quelqu'un qui est déjà connecté.

## card-front-route-guard-001-edge

**Question :** Pourquoi comparer les droits par correspondance exacte plutôt que par préfixe ?  
**Réponse attendue :** Parce qu'un préfixe laisserait orders.readonly satisfaire orders.read et
ouvrirait une porte sur une simple ressemblance de texte. La sécurité repose sur des correspondances
exactes ; un jeton dont l'expiration égale l'instant courant est d'ailleurs déjà considéré expiré.
