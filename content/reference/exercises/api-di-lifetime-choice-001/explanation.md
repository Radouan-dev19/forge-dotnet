# Explication

L'état de requête impose la durée courte ; un service partagé doit être explicitement sans état.

L'ordre des deux questions est la règle. L'état de requête tranche en premier parce qu'il interdit le partage : un service qui porte le contexte de l'appelant ne peut pas être détenu par un service qui vit plus longtemps. Inverser l'ordre produit la dépendance captive, où une instance courte est retenue pour la vie de l'application.

Le second critère demande deux propriétés simultanées : être sans état et être sûr en usage concurrent. Une seule des deux ne suffit pas — un service sans état mais coûteux à construire gagne au partage, un service partagé mais porteur d'un champ mutable sera corrompu par l'accès concurrent. La décision est en temps constant.
