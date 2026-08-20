# Cartes de révision

- **Par quel module commence une migration par étranglement, et pourquoi ?** Par le moins appelé :
  chaque dépendance entrante est un appelant à repointer, et les premières extractions bon marché
  construisent l'outillage et la confiance avant les chantiers durs.
- **Pourquoi le plan d'extraction se recalcule-t-il après chaque bascule ?** Parce que chaque
  extraction retire des entrantes aux modules restants : les comptes qui ont produit le plan ne sont
  plus vrais, et le module central d'aujourd'hui sera abordable quand son tour viendra.
