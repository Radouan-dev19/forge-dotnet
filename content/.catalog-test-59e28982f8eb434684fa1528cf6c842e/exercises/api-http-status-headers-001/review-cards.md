# Cartes de révision

- **Quels statuts HTTP n'ont de sens qu'avec un en-tête d'accompagnement ?** La création et les
  redirections exigent l'adresse cible ; le refus d'étranglement exige le délai de retour — les
  servir nus laisse le client deviner la suite.
- **Pourquoi une écriture redirigée n'utilise-t-elle pas la redirection historique ?** Parce que
  beaucoup de clients la suivent en dégradant la méthode vers une lecture : l'écriture disparaît en
  route, et seule la redirection préservant la méthode l'évite.
