# Explication

Trois commandes qui semblent faire la même chose, et une seule est correcte selon la situation. La
question à trancher n'est pas « laquelle je connais » mais « ai-je le droit de réécrire ».

**Annuler n'est pas faire disparaître.** C'est le renversement conceptuel du sujet. L'annulation par
commit inverse *ajoute* un commit qui défait les modifications ; le commit fautif reste dans
l'historique, visible pour toujours. Beaucoup y voient un défaut et cherchent à « nettoyer ». C'est
précisément cette visibilité qui rend l'opération sûre : personne n'a besoin de rien démêler, parce
que rien n'a été retiré. L'historique raconte ce qui s'est passé — une erreur, puis sa correction —
et c'est une histoire honnête.

**Les deux retraits, eux, réécrivent.** Ils déplacent le sommet de la branche en arrière et
abandonnent des commits. Tant que ces commits n'existent que sur votre poste, l'opération est
invisible et sans risque. Dès qu'ils sont publiés, ils vivent également ailleurs : votre retrait ne
les efface pas là-bas, il crée une divergence. Le prochain qui synchronise verra son historique et le
vôtre raconter deux versions différentes du même passé, et devra choisir — souvent en poussant de
force, ce qui écrase le travail de quelqu'un. C'est pourquoi la publication se teste **en premier** :
placée après le souhait de conserver le travail, elle laisserait passer une réécriture interdite dans
le cas exact où elle protégeait.

**La différence entre les deux retraits est une question de filet.** Le retrait doux ramène les
modifications dans la copie de travail : le commit disparaît, le contenu reste, prêt à être
recommitté autrement — c'est le geste quotidien quand on s'est trompé de message ou de découpage. Le
retrait dur jette tout. Rien ne le rattrape : ni l'historique, qui ne contient plus ces commits, ni
la copie de travail, qui a été remise à l'état antérieur. Le choisir suppose de savoir que le travail
est perdu, et de le vouloir.

**Le refus d'un compte nul mérite un mot.** Annuler zéro commit pourrait être traité comme une
opération sans effet, silencieuse et inoffensive. C'en serait une mauvaise interprétation : personne
n'écrit sciemment « annule zéro commit ». La valeur vient d'un calcul, et un calcul qui donne zéro là
où l'on attendait un nombre est un défaut en amont. Le signaler au moment où l'information existe
encore vaut mieux que de laisser l'appelant croire que son annulation a eu lieu.

**Ce que le modèle ne dit pas**, et qu'il faut savoir nommer : « publié » n'est pas binaire dans la
réalité. Un commit poussé sur une branche personnelle que personne n'a récupérée est techniquement
public sans l'être. Le modèle force la prudence dans ce cas, et se trompe donc du côté sûr — une
règle qui protège trop coûte moins qu'une règle qui protège trop peu.

Le coût est constant : une garde et deux tests booléens.
