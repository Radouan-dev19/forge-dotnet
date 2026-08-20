# Cartes de révision

- **Comment consolider un journal de pipeline où les travaux se relancent ?** Par dernière entrée :
  le verdict d'un travail est son dernier statut consigné, et une relance réussie efface l'échec qui
  la précède.
- **Quand une annulation est-elle la cause du rouge plutôt que sa conséquence ?** Uniquement quand
  aucun travail ne porte d'échec final : sinon l'échec est la cause première et les annulations sont
  ses victimes.
