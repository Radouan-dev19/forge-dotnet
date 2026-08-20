# Cartes de révision

- **Quelle est la portée exacte de la garantie d'ordre d'un journal partitionné ?** Une partition :
  deux messages de la même clé sur la même partition arrivent dans l'ordre, éclatés sur deux ils
  arrivent au rythme de consommateurs indépendants.
- **Où se voit une perte d'ordre par partitionnement, et où ne se voit-elle pas ?** Dans le journal
  de routage — une clé, plusieurs partitions ; ni dans les horodatages, qui datent l'émission, ni
  dans le contenu des messages, intact.
