# Cartes de révision

- **Que filtre une alerte à persistance et que coûte-t-elle ?** Elle tait les pics isolés en exigeant
  des dépassements consécutifs, au prix d'un retard de détection égal à la série exigée moins un
  échantillon — un taux de change à budgéter.
- **Pourquoi un échantillon calme remet-il la série à zéro ?** Parce que consécutif est le cœur du
  contrat : un compteur qui survivrait aux accalmies déclencherait sur des pics disjoints, le bruit
  exact que le réglage devait taire.
