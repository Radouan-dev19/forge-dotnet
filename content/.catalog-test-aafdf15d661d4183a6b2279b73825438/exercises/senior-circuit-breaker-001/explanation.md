# Explication

Le disjoncteur est un petit automate a trois etats, et toute la difficulte tient a ce que la
signification d'un meme evenement change selon l'etat courant. Un echec n'a pas le meme poids
selon qu'on est dans un fonctionnement normal ou dans une periode de convalescence.

L'etat `closed` est le regime nominal : les appels passent. On y compte les echecs consecutifs,
et un succes efface ce compteur, parce qu'un unique echec isole au milieu d'appels reussis ne
temoigne pas d'une panne installee. Ce n'est que lorsque les echecs s'accumulent jusqu'au seuil
que l'automate bascule en `open` : le circuit se coupe pour cesser de marteler un service deja a
terre et pour rendre la main tout de suite au lieu d'attendre des delais qui expirent.

L'etat `open` est deliberement sourd. Il ignore aussi bien un `ok` qu'un `fail`, parce que dans ce
regime aucun appel reel n'est envoye : il n'y a donc rien a observer, et un evenement qui pretend
le contraire ne doit rien changer. Seul l'ecoulement du temps, modelise ici par le jeton `tick`,
fait evoluer cet etat. Confondre ce silence avec une simple accumulation d'echecs est l'erreur qui
transforme un disjoncteur en compteur naif.

Le jeton `tick` fait passer de `open` a `half-open`. C'est la phase de convalescence : on laisse
passer un essai unique pour tester si le service distant est retabli. Cet etat est le plus subtil,
car un seul evenement y decide de tout. Un `ok` prouve le retablissement et referme le circuit ;
un `fail`, lui, doit rouvrir immediatement, sans attendre un quelconque seuil. La raison est nette :
en `half-open`, un echec signifie que la reparation supposee a echoue, et reprendre le trafic
normal reviendrait a rouvrir la vanne sur un service encore casse. Traiter cet echec probatoire
comme un echec ordinaire, qui se contente d'incrementer un compteur, laisserait le circuit ferme a
tort. Le cas cache qui enchaine ouverture, `tick`, puis `fail` verifie precisement cette bascule.

La remise a zero du compteur accompagne chaque changement d'etat. Sans elle, des echecs comptes
dans une vie anterieure du circuit contamineraient la suivante, et le seuil se declencherait pour
de mauvaises raisons. Le compteur n'a de sens que dans l'etat `closed` ; ailleurs, il doit rester
neutralise.

Le cout est lineaire dans le nombre de jetons : une seule passe suffit, l'etat resumant tout le
passe pertinent. La transposition depasse le disjoncteur : c'est le principe general de la machine
a etats, ou l'on refuse de deduire une action du seul evenement recu et ou l'on exige toujours de
lire d'abord l'etat courant.
