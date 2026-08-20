# Explication

Après la connexion, l'application renvoie l'utilisateur « là où il allait » — un paramètre de
retour que le client fournit. Ce paramètre est le siège d'une vulnérabilité classée : la
redirection ouverte, et l'énoncé demande ce qu'elle permet de faire d'un utilisateur
authentifié. La réponse est le scénario d'hameçonnage parfait : un lien vers *votre vrai site*,
avec un paramètre de retour vers le site du pirate — l'utilisateur voit votre domaine, se
connecte sur votre vraie page, et atterrit, confiance en poche, sur une réplique qui lui
demande de « ressaisir » son mot de passe. La redirection est le seul maillon fourni par
l'attaquant, et c'est donc elle qu'on verrouille.

La défense retenue est la plus stricte : n'accepter que les chemins *locaux* — un séparateur en
tête, donc relatif au site courant. Toute adresse absolue — avec schéma et hôte — échoue au
premier test, ce qui écarte l'attaque frontale. Restent les deux formes sournoises, et ce sont
elles qui justifient l'exercice : `//evil.example` est une adresse *relative au protocole* —
les navigateurs la résolvent vers un autre hôte avec le schéma courant — et `/\` en est la
variante à séparateur inversé, que certains navigateurs normalisent en double barre. Les deux
commencent par une barre ; les deux sortent du site. D'où le triplet de conditions : commence
par la barre, mais ni double barre, ni barre-contre-oblique. Un contrôle qui s'arrêterait au
premier test — « commence par un séparateur, donc local » — serait exactement la fausse
sécurité que les cas cachés réfutent.

La leçon de méthode est là : la validation d'une donnée interprétée par *un autre logiciel* —
ici le navigateur — doit connaître les tolérances de cet interpréteur, pas seulement la
grammaire officielle. Les attaques vivent dans l'écart entre ce que la spécification dit et ce
que les navigateurs acceptent ; les formes à double séparateur sont l'exemple canonique.

Le régime d'erreur est le verdict calme : l'entrée absente ou blanche répond faux — pas de
retour fourni, l'appelant redirigera vers sa page par défaut — et la comparaison est ordinale,
les préfixes techniques ne se comparant jamais culturellement.

Les cas suivent l'énoncé : le chemin local qui passe, l'adresse absolue refusée, la double
barre et la barre inversée refusées.

Le coût est constant. La transposition est le principe de la liste blanche appliqué aux
destinations : un paramètre de redirection ne se « nettoie » pas, il se contraint — chemins
locaux seulement, ou liste fermée de destinations nommées. Et le corollaire de revue : toute
redirection dont la cible vient de la requête est un point chaud à contrôler, dans son propre
code comme dans celui des autres.
