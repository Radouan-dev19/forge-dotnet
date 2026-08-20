# Explication

La politique d'immuabilité des images répond à une question que beaucoup d'équipes ne se posent
qu'après leur premier incident : quand le manifeste dit « déployer la version un point quatre », que
garantit-il exactement ? La réponse honnête est : rien. Une étiquette est un pointeur mutable dans un
registre ; quiconque a le droit d'écrire peut la déplacer, volontairement — un correctif republié sous
le même numéro — ou non — une compromission de la chaîne de livraison. L'empreinte, elle, est le
condensat du contenu : elle ne peut pas désigner autre chose que ce qu'elle désigne. C'est la
différence entre citer un document par son titre et le citer par sa somme de contrôle.

**Pourquoi la version complète est refusée aussi.** C'est le point qui surprend : une étiquette de
version à trois nombres a l'air d'un engagement. Mais le registre n'impose aucune immuabilité aux
étiquettes — la convention qui veut qu'on ne republie pas une version est une politesse, pas une
garantie. La politique trace la ligne au seul endroit vérifiable : le contenu. Accepter la version
complète « parce que l'équipe est sérieuse » réintroduirait exactement la confiance implicite que la
politique existe pour supprimer. L'étiquette reste utile — lisible, cherchable — mais comme
accompagnement d'une empreinte, jamais comme adresse.

**Pourquoi le port du registre piège les contrôleurs naïfs.** La grammaire des références fait
cohabiter deux usages du deux-points : séparateur de port dans la partie registre, séparateur
d'étiquette en fin de nom. Un contrôle qui cherche « un deux-points quelque part » classerait
`registry.local:5000/app` comme étiquetée alors qu'elle est nue — et le refus, portant la mauvaise
raison, enverrait la personne corriger le mauvais champ. La règle de position — une étiquette ne peut
suivre qu'après la dernière barre oblique — est la seule lecture compatible avec la grammaire réelle.

**Pourquoi l'empreinte mal formée a sa propre raison.** Une empreinte tronquée ou en majuscules
signale presque toujours un copier-coller abîmé, pas une intention de flotter. La distinguer de
l'étiquette seule change le geste de correction : recopier l'empreinte, plutôt que d'aller en produire
une. Un contrôleur qui fusionne les deux verdicts fait perdre ce temps de diagnostic à chaque erreur —
et la validation stricte de l'alphabet évite le pire des cas, l'empreinte « presque bonne » acceptée
puis refusée par le registre au moment du tirage, en pleine fenêtre de déploiement.

**Pourquoi lister au lieu de bloquer au premier refus.** Le contrôle sert un correcteur humain : lui
livrer toutes les violations d'un coup, dans l'ordre du fichier et avec leur raison, transforme trois
allers-retours en un seul. La chaîne vide en cas de conformité donne au passage un signal net aux
chaînes d'intégration — rien à dire, porte ouverte.

La transposition dépasse les conteneurs : dépendances de paquets, actions de chaînes d'intégration,
modules d'infrastructure — tout ce qui se référence par étiquette mutable gagne à être épinglé par
contenu.
