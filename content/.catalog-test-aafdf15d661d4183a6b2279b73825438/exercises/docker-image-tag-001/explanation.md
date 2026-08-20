# Explication

Une étiquette d'image n'est pas un identifiant : c'est un **pointeur**, et un pointeur se déplace.
Celle qui désignait une image ce matin peut en désigner une autre ce soir, sans qu'aucune trace n'en
soit conservée dans ce que l'on déploie. Tout l'exercice découle de cette propriété.

**Ce que l'on perd en production.** Deux serveurs qui démarrent la même référence à une heure
d'écart peuvent exécuter deux codes différents. Le journal dira qu'ils font tourner la même chose ;
la réalité sera autre. Et lorsque l'incident survient, la question « quelle version tournait » n'a
plus de réponse : l'étiquette a bougé, l'image d'origine n'est peut-être plus référencée nulle part.
Un retour arrière devient un pari. C'est pourquoi le refus porte sur l'environnement de production
seulement — hors production, la commodité de toujours prendre la dernière l'emporte, et le coût d'une
incertitude est nul.

**L'étiquette implicite est le vrai piège.** Une référence sans deux-points ne signifie pas « sans
étiquette » : elle signifie l'étiquette mouvante. `app` et `app:latest` désignent exactement la même
chose. Une implémentation qui n'examine que les références comportant un deux-points laisse donc
passer le cas le plus fréquent en production — celui où quelqu'un a simplement oublié de préciser la
version. Le contrôle qui semblait protéger ne protège que les gens déjà prudents.

**Le dernier deux-points, jamais le premier.** Un registre privé s'écrit couramment avec un port :
`registry.local:5000/app:1.4.2`. Chercher le premier séparateur ferait lire `5000/app:1.4.2` comme
étiquette — une valeur qui n'est pas la mouvante, donc le contrôle passerait sans rien dire, sur
toutes les références de ce registre. Un défaut silencieux, qui ne se manifeste que là où le registre
est privé, c'est-à-dire souvent en production et rarement sur le poste de développement.

**La comparaison ignore la casse** parce que le contrôle porte sur une convention, pas sur une
syntaxe. Une référence écrite en capitales déplace exactement autant, et la refuser en fonction de
son orthographe reviendrait à laisser une porte ouverte pour une raison purement typographique.

**La fonction rend la référence plutôt qu'un booléen**, et c'est délibéré : elle détoure au passage,
si bien que l'appelant reçoit une valeur utilisable et non un verdict à interpréter. Une fonction de
validation qui rend la valeur validée s'insère dans une chaîne de traitement ; une qui rend vrai ou
faux oblige à conserver l'original et à le nettoyer une seconde fois.

**Ce que le modèle simplifie** : la garantie réelle de reproductibilité vient de l'empreinte de
l'image, pas de son étiquette. Une version explicite peut elle aussi être déplacée par quelqu'un qui
republie sous le même numéro. L'étiquette versionnée est une convention de discipline ; l'empreinte
est une preuve. Savoir citer cette distinction vaut mieux que de croire le problème entièrement
résolu.
