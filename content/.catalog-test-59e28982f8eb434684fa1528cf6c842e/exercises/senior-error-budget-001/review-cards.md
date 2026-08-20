# Cartes de révision

## card-senior-error-budget-001-rule

**Question :** Comment obtient-on le nombre d'echecs autorises a partir d'un SLO en points de base ?  
**Réponse attendue :** On prend la tolerance, 10000 moins le SLO, on la multiplie par le volume de requetes et on divise par 10000 en entier ; le budget est epuise quand les echecs observes depassent strictement ce nombre.

## card-senior-error-budget-001-edge

**Question :** Que decide-t-on quand les echecs observes egalent exactement le budget autorise ?  
**Réponse attendue :** On livre encore : l'epuisement exige un depassement strict, donc etre pile au budget n'est pas le franchir.
