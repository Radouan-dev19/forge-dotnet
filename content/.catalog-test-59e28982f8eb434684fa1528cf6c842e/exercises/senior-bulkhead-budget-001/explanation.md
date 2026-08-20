# Explication

Le cloisonnement répond à une question que le disjoncteur ne pose pas : combien une dépendance
lente a-t-elle le **droit de coûter** au processus qui l'appelle ? Un service aval qui ralentit ne
renvoie pas d'erreur — il retient. Chaque requête en attente immobilise un fil d'exécution, une
connexion, de la mémoire ; sans cloison, une seule dépendance dégradée absorbe tout le processus, et
l'incident se propage à des fonctionnalités qui n'avaient rien à voir avec elle. La cloison — comme
celle d'une coque de navire, l'image qui a donné son nom au motif — borne la voie d'eau au
compartiment.

**Pourquoi le rejet rapide est la réponse saine et non un échec.** L'intuition proteste : rejeter une
requête, c'est échouer. Mais comparez les deux issues quand tout est plein. L'attente illimitée
retient l'appelant — qui retient son propre appelant — et reconstruit, cloison par cloison, la
propagation que le motif devait empêcher ; l'incident revient, avec une étape de plus. Le rejet
rapide rend la main en microsecondes : l'appelant peut dégrader — une valeur en cache, une réponse
partielle, un message d'indisponibilité — et le processus garde ses ressources pour ce qui
fonctionne. Un rejet de cloison n'est pas la panne : c'est le signal qui empêche la panne de
voyager.

**Pourquoi l'ordre exécution puis file n'est pas interchangeable.** Mettre en file une requête alors
qu'un emplacement d'exécution est libre lui fait payer une attente pour rien et désynchronise l'état
du cloisonnement de sa réalité. La cascade vérifie donc l'exécution d'abord ; la file n'existe que
pour absorber les pointes courtes quand les emplacements sont tous pris — et sa capacité nulle est un
réglage légitime, le plus strict : certains appels critiques préfèrent un refus immédiat à toute
attente, parce que leur appelant a lui-même un budget de temps.

**Pourquoi les relevés impossibles se refusent.** Onze exécutions en cours pour dix emplacements ne
décrivent aucun instant réel : c'est un compteur qui a fui — décrément oublié sur une exception,
lecture non synchronisée. Rendre un verdict dessus produirait une décision plausible sur un état
imaginaire, et le compteur fuyard resterait invisible. Le refus fait remonter le vrai défaut, qui
n'est pas dans la politique d'admission mais dans la comptabilité. La distinction entre les deux
familles de refus dit la même chose autrement : une capacité absurde est une erreur de configuration
à corriger dans un fichier, une occupation absurde est un défaut de code à corriger dans le
compteur.

En entretien, ce motif se nomme bulkhead, et il se combine avec le disjoncteur : la cloison borne le
coût pendant que le disjoncteur décide de couper. Les bibliothèques de résilience fournissent les
deux ; savoir dire lequel protège quoi — la cloison protège l'appelant, le disjoncteur protège
l'appelé — est précisément la réponse attendue.
