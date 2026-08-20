# Explication

Choisir la durée de vie d'un service — scoped, singleton ou transient — est une décision que
tout développeur d'API prend des dizaines de fois, souvent par imitation. L'exercice la réduit
à ses deux questions décisives et impose leur ordre, parce que c'est l'ordre qui protège.

Première question : le service porte-t-il de l'état de requête ? Si oui, scoped, et la
discussion s'arrête — d'où la garde en tête, qui court-circuite tout le reste. La raison est
une règle de sûreté : un singleton qui retient de l'état de requête mélange les requêtes entre
elles — le panier d'un client apparaît chez un autre, l'identité d'un appel fuit dans le
suivant. C'est le pire bug de cette famille : invisible en développement où les requêtes
s'enchaînent une à une, catastrophique en charge où elles s'entrelacent. La priorité du critère
d'état dans le code — le premier `if` — est la transcription de la gravité du risque : quand
les deux indicateurs sont vrais, l'état de requête l'emporte, et le cas caché sur cette
combinaison le fige.

Deuxième question, posée seulement quand la première a dit non : le service est-il *sans état
et* partageable ? Alors singleton — une instance pour tout le processus, le choix le plus
économe : pas de construction répétée, pas de pression sur le ramasse-miettes. La conjonction
compte : « partagé » sans « sans état » est exactement le piège que la première question
écarte, et l'énoncé le dit — un service partagé doit être *explicitement* sans état, pas
supposé tel.

Le reste — ni état de requête, ni partage sûr — tombe en transient : une instance par
résolution, le choix par défaut de la prudence. Il coûte des allocations et ne promet rien,
c'est sa force : un transient ne peut pas fuiter d'état, puisqu'il n'en garde pas l'occasion.

La forme de la fonction — deux booléens vers un libellé — est un arbre de décision assumé, et
ses quatre feuilles s'énumèrent : l'énoncé demande d'écrire les quatre combinaisons avant de
coder, et les cas les couvrent. C'est le domaine d'entrée fini assumé de l'exercice : la
valeur est dans la *justification* de chaque feuille, pas dans la variété des entrées.

La transposition est immédiate et quotidienne : chaque enregistrement dans le conteneur
d'injection devrait pouvoir répondre aux deux questions dans cet ordre — état de requête ?
partage sans état ? — et le mauvais choix a des symptômes nommables en entretien : le
singleton à état qui mélange les utilisateurs, le scoped injecté dans un singleton qui
survit à sa requête, le transient coûteux construit mille fois. Savoir dérouler cet arbre à
voix haute, c'est exactement ce que la question d'entretien associée attend.
