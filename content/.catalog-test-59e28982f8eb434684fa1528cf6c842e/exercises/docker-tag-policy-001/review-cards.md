# Cartes de révision

- **Pourquoi une étiquette de version complète ne suffit-elle pas à déployer ?** Parce qu'une
  étiquette est un pointeur mutable du registre : seule l'empreinte de contenu garantit de tirer
  demain exactement ce qui a été validé aujourd'hui.
- **Comment distinguer le deux-points d'un port de celui d'une étiquette ?** Par la position : une
  étiquette ne peut suivre qu'après la dernière barre oblique de la référence ; avant elle, le
  deux-points appartient au registre.
