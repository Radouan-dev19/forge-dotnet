# Explication

L'idempotence est la propriété la plus rentable du protocole, et la moins comprise : une méthode
est idempotente quand *la répéter produit le même état* que l'exécuter une fois. L'exercice fait
classer les méthodes, et la classification n'a de valeur que si l'on sait la justifier méthode
par méthode — c'est ce que la question d'entretien associée demande.

Le déroulé : la lecture ne change rien, donc la répéter ne change rien — idempotente, comme ses
cousines d'en-têtes et d'options. La mise à jour intégrale écrit *un état complet* : l'écrire
deux fois laisse le même état — idempotente, et c'est la plus contre-intuitive du lot, une
écriture qui se rejoue sans dégât. La suppression détruit une fois ; la rejouer trouve le vide
et ne détruit rien de plus — l'état final est identique, même si le *statut* de la seconde
réponse peut différer : l'idempotence parle de l'état du serveur, pas de la réponse. La
création, elle, est la grande absente de la liste : la rejouer crée un doublon — deux
commandes, deux paiements — et c'est précisément pourquoi elle n'y figure pas. La modification
partielle n'y est pas non plus : un incrément relatif rejoué s'additionne.

Pourquoi cette propriété vaut de l'or, l'énoncé le demande : *le réessai sans risque*. Un
client qui n'a pas reçu de réponse — délai, coupure — ne sait pas si sa requête a été traitée.
Si la méthode est idempotente, il réessaie aveuglément : au pire, il refait ce qui était fait.
Si elle ne l'est pas, réessayer risque le doublon, et il faut un mécanisme supplémentaire — une
clé d'idempotence portée par la requête. Toute la robustesse des intergiciels de réessai, des
mandataires et des files repose sur cette classification : c'est elle qui dit ce qui peut être
rejoué par la machinerie sans demander la permission.

Le code est une liste blanche sur identifiant normalisé — bords rognés, majuscules
invariantes, motif `is ... or` — et l'inconnu tombe en non-idempotent : le refus est le défaut
sûr, exactement comme dans la table des statuts d'erreur voisine. Une méthode inventée, ou
absente, n'autorise aucun réessai aveugle. Les cas cachés jouent la casse minuscule qui
converge, la création qui répond faux, et l'inconnue refusée.

Le coût est constant. La transposition dépasse le protocole : toute opération d'un système
distribué mérite la question « que se passe-t-il si elle est exécutée deux fois ? » — messages
d'une file consommés après un crash, tâches replanifiées, migrations relancées. Concevoir
idempotent quand c'est possible, et protéger par clé quand ça ne l'est pas : c'est l'un des
deux ou trois principes qui distinguent un système qui survit aux pannes d'un système qui les
amplifie.
