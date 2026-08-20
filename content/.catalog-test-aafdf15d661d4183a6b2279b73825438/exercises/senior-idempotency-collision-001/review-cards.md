# Cartes de révision

- **Qu'est-ce qu'une collision de clé d'idempotence et pourquoi est-elle silencieuse ?** Une clé
  réutilisée pour une opération différente : le serveur rejoue la réponse de la première et avale la
  seconde — aucune erreur nulle part, seul le rapprochement des empreintes la révèle.
- **Contre quelle empreinte se juge une clé revue ?** Contre l'empreinte de référence — la première
  vue, celle que le serveur mémorise avec la réponse — jamais contre la précédente : des relances
  légitimes intercalées masqueraient la collision.
