# Explication

La corrélation de traces promet une chose : reconstituer le voyage d'une requête à travers des
services qui ne se connaissent pas. Cette promesse tient à un fil — la propagation du contexte. Chaque
service doit recevoir l'identifiant du segment appelant et le noter comme parent du sien ; qu'un seul
maillon oublie l'en-tête, le transforme, ou passe par un canal qui ne le transporte pas — une file de
messages, une tâche de fond, un cache — et le segment aval arrive au collecteur sans attache. Il n'est
pas perdu : il est orphelin, ce qui est plus sournois. Les données existent, la facture de stockage
les compte, mais la cascade s'arrête au milieu et le temps passé en aval devient invisible.

**Pourquoi compter les orphelins plutôt que les chercher un par un.** Un orphelin isolé est une
anecdote ; un taux d'orphelins est un diagnostic. La propagation ne se dégrade presque jamais au
hasard : elle casse à un endroit précis — telle file, tel intergiciel, telle bibliothèque mise à
jour — et ce point de casse se voit dans la masse, pas dans l'exemplaire. Le comptage est la première
marche de toute surveillance de la qualité des traces elles-mêmes : on instrumente le système, puis on
instrumente l'instrumentation.

**Pourquoi il faut deux passages.** Le collecteur reçoit les segments dans l'ordre d'arrivée réseau,
et un enfant précède souvent son parent — le service aval, plus court, répond avant que l'amont ait
fini d'écrire son propre segment. Juger les liens à la volée, pendant la première lecture,
condamnerait tous les enfants précoces et gonflerait le compte de faux orphelins au rythme des
latences. Il faut d'abord connaître l'ensemble complet des identifiants, ensuite seulement juger
chaque lien. Ce schéma — collecter avant de résoudre — est celui de tout éditeur de liens, et il
revient partout où des références croisées arrivent en désordre.

**Pourquoi le segment auto-parent est orphelin.** L'identifiant qu'il cite existe — c'est le sien.
Mais le test d'existence n'est qu'un moyen ; la question de fond est : ce segment se raccroche-t-il à
une cause en amont ? Un cycle d'un seul nœud ne se raccroche à rien, et il trahit généralement un
copier-coller du mauvais champ dans l'instrumentation. Le laisser passer ferait mentir la cascade
d'une autre façon : elle afficherait un segment flottant qui prétend être sa propre origine.

**Pourquoi l'identifiant répété est refusé.** Deux segments sous le même nom rendent chaque lien vers
ce nom ambigu : le journal n'est plus recousable, et tout compte rendu serait une supposition. Le
refus distingue « le journal mesure une propagation abîmée » — un fait exploitable — de « le journal
est lui-même abîmé » — un problème d'amont.

La limite de l'exercice mérite d'être nommée : deux segments qui se citent mutuellement forment un
cycle sans racine que le comptage d'orphelins ne voit pas — chaque parent existe. Détecter les cycles
est l'étape suivante d'un contrôle de cohérence, avec un parcours de graphe que ce comptage prépare.
