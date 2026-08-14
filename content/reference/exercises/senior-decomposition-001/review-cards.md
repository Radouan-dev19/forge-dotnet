# Cartes de révision

## card-senior-decomposition-001-rule

**Question :** Quelle est la decision par defaut face a une demande de decoupage, et quelles conditions la renversent ?  
**Réponse attendue :** Par defaut on garde le monolithe ; seule la conjonction de plusieurs equipes, d'aucun deploiement couple et d'aucune table partagee justifie d'extraire un service.

## card-senior-decomposition-001-edge

**Question :** Pourquoi des tables partagees interdisent-elles l'extraction meme avec plusieurs equipes ?  
**Réponse attendue :** Parce que la frontiere passe alors au milieu des donnees : le service extrait reste couple par la base, un couplage cache plus dangereux que l'appel de methode qu'il remplace.
