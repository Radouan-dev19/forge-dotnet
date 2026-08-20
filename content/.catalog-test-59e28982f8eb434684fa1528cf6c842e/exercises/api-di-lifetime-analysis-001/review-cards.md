# Cartes de révision

- **Dans quel ordre se décident les durées de vie d'injection ?** L'état porté d'abord, la
  consommation d'un service de requête ensuite, le coût de construction en dernier : le coût ne
  départage que ce que les deux premières questions n'ont pas classé.
- **Qu'est-ce qu'une dépendance captive et que produit-elle ?** Un service longue durée qui consomme
  un service de requête : il fige la première instance reçue et la sert pour toujours — données
  périmées et connexions retenues, visibles seulement en production.
