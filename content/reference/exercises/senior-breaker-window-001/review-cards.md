# Cartes de révision

- **Pourquoi un disjoncteur exige-t-il un volume minimal avant de juger le taux d'échec ?** Parce
  que les fractions mentent sur les petits dénominateurs : deux échecs sur deux appels font cent
  pour cent, et un disjoncteur sans garde de volume coupe des services sains à chaque creux de
  trafic.
- **Que fait une sonde en échec pendant l'état demi-ouvert ?** Elle rouvre le circuit
  immédiatement, quel que soit le compte de sondes passées : fermer malgré elle réinjecte le trafic
  sur un service qui vient de répondre non.
