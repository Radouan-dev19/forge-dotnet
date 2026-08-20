# Explication

Une fonction qui ignore ses deux paramètres et rend toujours la même chaîne : c'est du code
volontairement dégénéré, et c'est le point — l'uniformité *est* la fonctionnalité. L'exercice
code une règle de sécurité dont la sortie constante se prouve par les quatre combinaisons
d'entrée.

L'énoncé demande ce que deux messages distincts permettraient d'apprendre : l'existence des
comptes. Si « utilisateur inconnu » et « mot de passe incorrect » se distinguent — dans le
texte, mais aussi dans le code de statut ou le moindre détail observable —, alors l'écran de
connexion devient un annuaire : l'attaquant soumet une liste d'adresses et lit, réponse par
réponse, lesquelles ont un compte. Cette énumération de comptes est la première étape des
attaques ciblées — on hameçonne mieux quand on sait qui est client — et sa correction coûte
une chaîne : le même « Identifiants invalides. », que l'échec vienne de l'identifiant ou de la
preuve. Le message unique ne dit qu'une chose — *ce couple-là n'ouvre pas* — et c'est
exactement l'information minimale qu'un échec de connexion doit porter.

Le code a une écriture singulière qui mérite explication : les affectations à la variable de
défausse — `_ = userExists` — disent au lecteur et à l'analyseur que les paramètres sont
ignorés *exprès*. Sans elles, un avertissement de paramètre inutilisé pousserait un mainteneur
bien intentionné à « corriger » en utilisant les indicateurs — recréant la fuite. La défausse
documente l'intention là où un commentaire seul pourrait être ignoré : c'est une barrière
contre la régression de sécurité par zèle.

Il faut aussi nommer ce que l'uniformité du message ne couvre pas : le *temps de réponse*. Si
le chemin « utilisateur inconnu » répond plus vite que le chemin « preuve vérifiée puis
refusée », la différence de latence trahit l'existence du compte aussi sûrement que deux
messages. Les implémentations sérieuses égalisent — en vérifiant une preuve factice même quand
l'utilisateur n'existe pas. C'est hors du périmètre de cette fonction pure, et c'est la
première question qu'un entretien posera derrière elle.

Les cas sont les quatre combinaisons, toutes vers la même chaîne — le domaine booléen fini,
couvert en entier, l'exception assumée du catalogue : ici, l'exhaustivité est triviale et
c'est l'uniformité qu'elle prouve.

La transposition dépasse la connexion : réinitialisation de mot de passe — « si ce compte
existe, un courriel a été envoyé » —, invitation, désinscription : partout où une réponse
peut confirmer une donnée privée, la réponse s'uniformise. La règle en une phrase : un échec
d'authentification ne renseigne jamais sur *pourquoi* il a échoué.
