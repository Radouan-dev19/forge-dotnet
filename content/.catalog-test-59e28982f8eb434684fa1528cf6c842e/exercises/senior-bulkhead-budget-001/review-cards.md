# Cartes de révision

- **Que borne un cloisonnement qu'un disjoncteur ne borne pas ?** Le coût qu'une dépendance lente
  impose au processus appelant : fils d'exécution et connexions retenus — la cloison protège
  l'appelant quand le disjoncteur protège l'appelé.
- **Pourquoi le rejet rapide d'une cloison saturée est-il une réponse saine ?** Parce que l'attente
  illimitée retient l'appelant et reconstruit la propagation que le motif devait empêcher : le rejet
  rend la main en microsecondes et permet de dégrader proprement.
