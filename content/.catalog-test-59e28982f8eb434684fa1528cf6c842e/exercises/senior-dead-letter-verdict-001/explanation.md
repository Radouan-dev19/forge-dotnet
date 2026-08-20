# Explication

La file des lettres mortes a mauvaise réputation — un cimetière de messages qu'on n'ose pas
regarder — parce qu'on la remplit mal. Bien remplie, c'est l'outil de diagnostic le plus précieux
d'une messagerie : chaque message y arrive avec la raison de sa sortie, et l'exploitant y lit l'état
de santé du système par familles. Cet exercice fige la discipline de remplissage, et l'ordre de ses
quatre questions n'a rien d'arbitraire.

**Le message illisible sort en premier, sans consommer de budget.** C'est le message empoisonné
classique : impossible à désérialiser, il échoue avant même que le traitement commence, et le
rejouer reproduit l'échec à l'identique — pas de réseau à attendre, pas de verrou à libérer, le même
octet fautif au même endroit. Le laisser consommer son budget de tentatives coûte doublement : la
file tourne en rond devant les messages sains, et la vraie information — un producteur a changé son
format — attend cinq échecs au lieu d'un. La première question de la cascade court-circuite tout,
y compris l'erreur déclarée : un relevé qui prétend à la fois « charge illisible » et « traitement
réussi » se tranche par la charge, parce qu'un traitement ne peut pas avoir réussi sur ce qu'il n'a
pas pu lire.

**L'erreur définitive ne se rejoue jamais, même budget ouvert.** La distinction passager-définitif
est le cœur du routage : un délai réseau se retente, une règle métier violée se retentera à
l'identique jusqu'à l'épuisement du budget — cinq échecs, cinq entrées de journal, cinq occasions de
réveiller quelqu'un, pour finir au même endroit. Router le définitif immédiatement économise tout
cela et, surtout, date correctement le problème : le message est aux lettres mortes à la première
tentative, pas à la cinquième, et l'horodatage de la file raconte la vérité.

**Le budget se compare strictement, et la frontière se teste.** La tentative qui atteint le maximum
est la dernière : la relancer ferait max plus une tentatives, et ce décalage d'une unité est
invisible partout sauf sur la facture — un budget de cinq qui en exécute six sur chaque message en
échec, à l'échelle d'une file, se voit en heures de calcul.

**Les raisons distinctes sont le contrat avec l'exploitant.** Une file de lettres mortes à raison
unique oblige à rouvrir chaque message pour comprendre ; une file à raisons nommées se traite par
lots — les charges illisibles remontent au producteur, les définitives à l'équipe métier, les budgets
épuisés à l'aval qui était indisponible. C'est la différence entre un cimetière et un centre de tri.

En entretien, les termes attendus sont message empoisonné et dead letter queue — et la question qui
suit est presque toujours celle de la surveillance : une file de lettres mortes qui grossit sans
alerte est un incident qu'on découvre des semaines trop tard.
