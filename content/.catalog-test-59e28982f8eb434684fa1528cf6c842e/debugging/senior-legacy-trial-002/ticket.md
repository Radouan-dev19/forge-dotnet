# Ticket — le bandeau d'essai annonce des jours negatifs

## Contexte

Le bandeau d'accueil affiche le nombre de jours restants de la periode d'essai. Le calcul vient d'un
module herite, sans test ni auteur encore present dans l'equipe.

## Symptome observe

Le support recoit des captures d'ecran ou le bandeau affiche **« -5 jours restants »**. Tous les
comptes touches ont depasse leur periode d'essai. Les comptes dont l'essai court encore affichent un
nombre plausible, et le jour meme du debut l'affichage montre la longueur entiere de l'essai.

## Attendu

Un essai de 14 jours commence le 1er mars et consulte le 20 mars doit afficher 0 jour restant : une
periode terminee reste terminee, quel que soit le retard de la consultation. Aucun nombre negatif ne
doit jamais atteindre le bandeau.

## Ce qui est demande

Reproduire le symptome avec une consultation posterieure a l'expiration, situer l'endroit ou le reste
devient negatif, corriger, puis figer un cas de non-regression sur un essai deja expire.
