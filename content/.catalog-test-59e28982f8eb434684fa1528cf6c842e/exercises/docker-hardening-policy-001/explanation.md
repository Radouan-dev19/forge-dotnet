# Explication

Trois réglages, une conjonction : le socle de durcissement d'un conteneur est complet ou il
n'est pas. La fonction tient en une ligne, et l'exercice vaut par la capacité à défendre
chaque terme de la conjonction — pourquoi ces trois-là, et pourquoi *ensemble*.

L'utilisateur non privilégié d'abord : un processus qui tourne en root dans le conteneur est
root face à toute faille d'évasion, et les fichiers montés depuis l'hôte lui appartiennent. Le
non-root est la réduction de surface la plus rentable — la plupart des applications n'ont
aucune raison légitime d'être privilégiées, et le laboratoire de conteneurisation du parcours
montre le réglage réel qui l'impose.

Le système de fichiers en lecture seule ensuite : un attaquant qui obtient l'exécution dans un
conteneur inscriptible peut y déposer des outils, modifier des binaires, persister. En lecture
seule, le conteneur redevient ce qu'il devait être — une image immuable qui exécute — et les
écritures légitimes se déplacent vers des volumes dédiés, explicites et bornés.

L'interdiction d'élévation enfin, la plus subtile, et l'énoncé demande précisément ce que son
absence permet malgré une identité non privilégiée : les binaires à bit de privilège. Un
exécutable marqué pour s'élever au lancement donne au processus non privilégié qui l'invoque
les droits de son propriétaire — c'est le mécanisme historique des commandes d'administration
— et une faille dans l'un de ces binaires refait le chemin vers root que le premier réglage
avait fermé. Le drapeau d'interdiction verrouille : aucun processus du conteneur ne pourra
jamais acquérir plus de privilèges que ceux du départ. Sans lui, le non-root est une porte
fermée dont la clé traîne dans la pièce.

D'où la conjonction stricte : chaque réglage couvre le trou que les autres laissent, et un
socle à deux tiers donne la confiance du durcissement sans sa réalité — plus dangereux que pas
de socle du tout, car il endort la vigilance. Les cas de l'énoncé le disent en creux : la
configuration complète passe, et chacun des trois retraits isolés échoue — huit combinaisons
au total, domaine booléen couvert, l'exhaustivité triviale assumée du catalogue.

Le coût est constant. La transposition est le concept de *ligne de base de sécurité* : un
ensemble nommé de réglages non négociables, vérifié mécaniquement — par ce prédicat dans une
chaîne de validation, par des politiques d'admission ailleurs — et dont toute exception est
une décision documentée, jamais un oubli. La sécurité par liste de vérification bat la
sécurité par bonne volonté, à chaque déploiement.
