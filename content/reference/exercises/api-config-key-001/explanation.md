# Explication

Coller deux segments avec un deux-points : l'opération semble indigne d'un exercice, et c'est
un jugement à réviser — les clés de configuration sont des chaînes magiques qui traversent tout
un système, et chaque défaut de composition devient une panne au démarrage ou, pire, une valeur
par défaut silencieuse en production.

Le séparateur d'abord : le deux-points est la convention hiérarchique du système de
configuration — les sections s'y emboîtent, et les fournisseurs la traduisent chacun dans leur
dialecte, double soulignement pour les variables d'environnement, imbrication pour le JSON.
Composer les clés par une fonction unique, plutôt qu'en concaténant à la main partout, garantit
que le séparateur est le bon *partout* — et qu'un changement de convention aurait un seul
endroit à visiter. C'est la première leçon : les chaînes magiques se fabriquent en un point.

La validation ensuite, et l'énoncé demande de nommer ce qu'une clé mal formée produit au
démarrage : au mieux une exception de liaison, au pire *rien* — la configuration introuvable
rend sa valeur par défaut, le service démarre avec un réglage fantôme et le défaut ne se voit
que sous charge, quand le délai par défaut ou l'URL par défaut se met à mordre. C'est pour cela
que la fonction refuse les segments absents, vides ou blancs par une exception immédiate — la
composition est le dernier moment où l'erreur est bon marché. La philosophie a un nom, échouer
tôt : toute erreur de configuration doit exploser au démarrage, quand un humain regarde, jamais
à la première requête qui exerce le chemin.

Le rognage des segments complète la robustesse : les blancs de bordure sont des artefacts de
copier-coller de fichiers de réglages, et `Authentication :ApiKey` avec son espace interne au
segment serait introuvable pour toujours. Rogner aux bords sans toucher l'intérieur — la
distinction habituelle — normalise ce que la saisie abîme sans réécrire ce que l'utilisateur a
voulu.

Les cas cachés jouent les trois axes : la paire propre qui compose, les bordures blanches qui
se rognent, et les segments vides ou blancs qui lèvent — chaque garde a son cas, l'exception
est du contrat, pas un accident.

Le coût est trivial ; l'enjeu ne l'est pas. La transposition dépasse la configuration : clés de
cache, chemins de ressources, identifiants composés — partout où des segments deviennent une
chaîne d'adressage, les trois mêmes règles s'appliquent. Un seul point de composition, une
validation qui refuse l'incomplet, un nettoyage des bords. Et une quatrième, en filigrane : la
constante qui nomme la clé composée vaut toujours mieux que la littérale recopiée deux fois.
