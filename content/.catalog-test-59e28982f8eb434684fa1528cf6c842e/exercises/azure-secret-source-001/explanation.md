# Explication

Où vit une valeur de configuration ? La réponse dépend de deux questions posées dans le bon
ordre, et cette petite fonction est l'arbre de décision que chaque équipe devrait pouvoir
réciter.

La première question tranche : la valeur est-elle *sensible* ? Une URL de service, une taille
de page, un drapeau de fonctionnalité — non sensibles — vivent en configuration ordinaire :
fichiers de réglages, variables d'environnement, versionnés et lisibles, car leur fuite ne
coûte rien et leur lisibilité vaut de l'or en diagnostic. Classer *tout* en secret est une
erreur symétrique de ne rien classer : les vrais secrets se noient dans les faux, et la
friction pousse aux contournements. La sensibilité se juge par le coût de la fuite — un
attaquant qui lit cette valeur gagne-t-il quelque chose ?

Pour les valeurs sensibles, la deuxième question choisit le mécanisme, et l'énoncé demande ce
que l'identité attestée par la plateforme supprime *entièrement* : le secret d'amorçage. Le
paradoxe classique des coffres à secrets est qu'il faut un secret pour y accéder — une clé
d'API du coffre, qui doit bien vivre quelque part, et le problème recommence. L'identité
gérée coupe la boucle : la plateforme d'exécution atteste elle-même « ce processus est bien
l'application X », et le coffre accorde l'accès à cette identité — aucune clé à stocker, à
faire tourner, à faire fuir. C'est la seule réponse au problème de l'amorçage qui le supprime
au lieu de le déplacer, et c'est pourquoi elle gagne dès qu'elle est disponible.

Quand elle ne l'est pas — le poste de développement, typiquement — le repli est le magasin
local de secrets utilisateur : hors de l'arborescence du projet, donc hors du dépôt par
construction, là où le fichier de réglages « temporaire » finit toujours commité. Le trio de
sorties dessine ainsi la politique complète : configuration pour l'ordinaire, coffre par
identité pour la production, magasin local pour le développement — et *aucun* chemin ne mène
au dépôt de sources.

Les quatre combinaisons s'énumèrent, l'énoncé les fait écrire, les cas les couvrent — la
non-sensible ignore la disponibilité de l'identité, ce que les deux cas correspondants
prouvent.

Le coût est constant. La transposition est l'audit de configuration d'un projet réel : lister
les valeurs, poser la question de sensibilité sur chacune, vérifier que chaque sensible suit
sa branche — et traiter tout secret trouvé dans l'historique du dépôt comme déjà compromis :
rotation, pas suppression, car l'historique n'oublie rien.
