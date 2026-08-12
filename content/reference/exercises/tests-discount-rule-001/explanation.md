# Explication

Couvrir chaque partition et les frontières exactes des deux paliers.

L'ordre d'évaluation décide de la correction : les paliers se testent du plus élevé au plus bas, sinon le premier test attrape aussi les montants qui relevaient du palier supérieur, et la branche haute devient inatteignable. Aucun avertissement ne le signale.

Les comparaisons sont larges, donc un montant exactement égal à un palier bénéficie de la remise annoncée. C'est une décision commerciale autant que technique, et la seule façon de la figer est de tester la valeur du palier ainsi que celle qui la précède immédiatement. Un jeu de tests construit sur une valeur par partition, sans toucher les paliers, laisse passer toute erreur de comparaison. La décision est en temps constant.
