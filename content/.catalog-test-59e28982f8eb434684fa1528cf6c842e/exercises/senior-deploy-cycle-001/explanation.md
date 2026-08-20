# Explication

Le monolithe distribué ne s'annonce pas : il s'installe, un appel « temporaire » à la fois, jusqu'au
jour où deux services ne peuvent plus être livrés qu'ensemble. Le cycle d'appels est sa signature
formelle — et sa détection mérite d'être mécanique, parce que l'œil humain rate les cycles longs et
que chaque revue d'architecture devrait commencer par ce relevé.

**Pourquoi le cycle est le critère, et pas la simple dépendance.** Une chaîne d'appels — l'interface
appelle l'inventaire qui appelle le référentiel — crée un ordre : on livre l'aval avant l'amont, on
teste de bas en haut, une panne se propage dans un seul sens. C'est de la dépendance, gérable. Le
cycle abolit l'ordre : chacun des services soudés a besoin que l'autre soit déjà là — pour livrer,
pour tester, pour démarrer après un incident. La version déployée de l'un contraint celle de l'autre
dans les deux sens, ce qui est la définition opérationnelle d'un seul déployable. D'où la nuance que
l'énoncé souligne : mener à un cycle n'est pas y être — l'interface qui appelle deux services soudés
garde, elle, sa liberté de livraison. Un détecteur qui classerait tout le bassin versant du cycle
noierait le signal.

**Pourquoi la détection par atteignabilité suffit ici.** Un service est en cycle si et seulement si
un chemin part de ses appels sortants et revient à lui. Le parcours depuis les successeurs, avec un
ensemble de visités — sans quoi on tourne dans le cycle qu'on cherche —, répond service par service.
Sur les graphes d'appels réels — quelques dizaines de services —, ce parcours répété est largement
suffisant ; les algorithmes de composantes fortement connexes font mieux asymptotiquement, et
l'entretien qui pousse dans cette direction attend surtout que le candidat sache que « les services
en cycle » et « les composantes fortement connexes de taille supérieure à un, plus les auto-appels »
sont le même ensemble.

**Ce qu'on fait d'un cycle détecté.** Le relevé n'est que le début ; la correction choisit entre
trois gestes, du moins cher au plus lourd. Inverser un des deux appels en événement : la facturation
n'appelle plus le catalogue, elle écoute ses publications — le cycle devient une chaîne. Répliquer
les données consultées : l'appel synchrone disparaît au profit d'une copie locale rafraîchie — même
effet. Ou admettre que la frontière était fausse et fusionner les services : douloureux pour
l'orgueil, honnête pour l'exploitation. Le pire choix est le quatrième, implicite : garder le cycle
et synchroniser les livraisons à la main — c'est lui, le monolithe distribué en régime permanent.

**Les refus gardent le relevé honnête.** Une arête répétée signale un graphe généré deux fois puis
concaténé ; une arête sans ses deux bouts ne décrit aucun appel. Le relevé trié, lui, se compare
d'un trimestre à l'autre : les cycles qui apparaissent sont la dette d'architecture en train de se
contracter.

En entretien, ce sujet se nomme par ses deux termes — dependency cycle et distributed monolith — et
la meilleure réponse enchaîne détection, nuance du bassin versant, et les trois gestes de correction.
