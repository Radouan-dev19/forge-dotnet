# Cartes de révision

- **Que casse le jitter que le recul exponentiel nu ne casse pas ?** La synchronisation entre
  clients : sans lui, tous les clients tombés ensemble relancent ensemble, et le serveur
  convalescent reçoit des vagues — le troupeau tonnant.
- **Pourquoi la politique du jitter égal plancher l'attente à la moitié de la fenêtre ?** Parce
  qu'un tirage sur la fenêtre entière autorise une attente proche de zéro — une relance en rafale,
  précisément ce que le recul devait empêcher.
