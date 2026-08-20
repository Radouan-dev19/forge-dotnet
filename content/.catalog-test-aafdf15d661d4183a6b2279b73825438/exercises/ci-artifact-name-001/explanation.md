# Explication

Nommer l'artefact qu'une exécution de chaîne d'intégration produit : le nom compose un préfixe
de type, une branche normalisée et un numéro d'exécution. La fonction est une concaténation ;
sa valeur est l'*archéologie* qu'elle rend possible — la question de l'énoncé : que permet de
retrouver un nom d'artefact six mois plus tard ?

Réponse : la provenance complète. Un rapport de tests nommé `tests-fix-payment-1847` dit ce
qu'il est, d'où il vient et de quelle exécution — sans ouvrir l'archive, sans interroger le
système de construction, peut-être disparu ou purgé depuis. Le jour où un audit demande « quels
tests ont validé cette version ? », le nom répond ; l'artefact nommé `resultats.zip` ou
`build-final-v2` ne répond de rien. La règle générale : un artefact est un objet qui survit à
son contexte de production — son nom doit porter le contexte avec lui.

La normalisation de la branche traite les trois accidents de ce domaine. Le séparateur de
chemin d'abord — les branches se nomment `feature/paiement`, et une barre oblique dans un nom
d'artefact devient un répertoire, cassant le dépôt de fichiers ou l'archivage : le remplacement
par un tiret rend le nom *plat*, sûr partout. La casse ensuite, aplanie en minuscules
invariantes : les systèmes de fichiers divergent sur la sensibilité à la casse, et deux
artefacts qui ne diffèrent que par elle seraient deux objets ici, un seul là — la forme
canonique évite l'ambiguïté. Les bords rognés enfin, l'hygiène habituelle des identifiants
saisis.

La validation refuse ce qui n'a pas d'identité : une branche absente ou blanche, un numéro
d'exécution nul ou négatif — le numéro un est le plus petit accepté, le cas de frontière posé
par l'énoncé. Un artefact anonyme est plus dangereux qu'une exécution en échec : il *existe*,
il sera consommé, et personne ne saura jamais d'où il venait.

Le numéro d'exécution mérite sa note : c'est lui qui rend le nom *unique* — deux exécutions
sur la même branche produisent deux artefacts distincts, jamais un écrasement silencieux — et
c'est le système de construction qui le fournit, monotone, hors de portée des collisions
humaines.

Les cas suivent l'énoncé : la branche simple, celle au séparateur, celle en majuscules, le
plus petit numéro, le nul refusé.

Le coût est trivial. La transposition est la politique de nommage de tout ce qu'une chaîne
produit — images, paquets, rapports, journaux archivés : type, provenance normalisée,
identifiant d'exécution, dans un gabarit unique et versionné. Six mois plus tard, quelqu'un
vous remerciera — probablement vous.
