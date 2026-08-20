# Cartes de révision

- **Pourquoi ne pas s'arrêter au premier défaut ?** Le serveur détient tout ce qui bloque dès le
  premier appel ; le taire impose au client un aller-retour par champ fautif.
- **D'où vient l'ordre du rapport ?** De la déclaration des champs attendus. Le faire suivre l'ordre
  du corps reçu rendrait deux appels équivalents incomparables entre eux.
