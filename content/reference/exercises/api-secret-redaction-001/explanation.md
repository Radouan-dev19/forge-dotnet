# Explication

Ne conserver aucun caractère du secret et produire un nombre minimal de marqueurs.

Masquer partiellement est une pratique répandue et discutable : les quelques caractères laissés visibles réduisent l'espace de recherche, et deux journaux issus de contextes différents peuvent se recouper. Ici le contrat est net — aucun caractère d'origine ne survit.

Le minimum de marqueurs traite une fuite plus discrète : la longueur du masque révèle celle du secret. Un plancher empêche de distinguer un secret très court d'un secret ordinaire. Et la règle ne vaut que si la rédaction précède toute écriture : un masque appliqué après la journalisation ne protège rien. Le coût est linéaire dans la longueur produite.
