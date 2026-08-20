# Explication

La stratégie de l'étrangleur doit son nom au figuier qui enveloppe un arbre et le remplace sans
jamais l'abattre : le système hérité continue de servir pendant que ses modules, un à un, renaissent
à côté. Son échec le plus courant n'est pas technique — c'est un échec d'ordonnancement, et le plan
de cet exercice encode les trois leçons qui l'évitent.

**Les entrantes mesurent le coût, et c'est le coût qui ordonne.** Le jour où un module sort, chacun
de ses appelants doit être repointé vers la nouvelle implémentation — routage, façade, double
écriture le temps de la bascule. Un module à zéro entrante s'extrait sans déranger personne ; le
module central à cinquante entrantes exige une campagne. L'intuition inverse — commencer par le cœur,
« le plus important » — est le piège classique : le chantier le plus cher est confié à une équipe
qui n'a encore jamais extrait, et l'échec de cette première extraction condamne la stratégie entière
dans l'esprit de l'organisation. L'ordre croissant des entrantes fait l'inverse : les premières
extractions sont bon marché, elles construisent l'outillage — façades, redirections, tableaux de
bord de bascule — et la confiance, pour le jour des chantiers durs.

**Les sortantes départagent, parce qu'elles mesurent l'autonomie.** Entre deux modules aussi peu
appelés, celui qui dépend le moins des autres vit mieux une fois seul : moins d'appels retour vers
le système hérité, moins de latence ajoutée, des tests plus francs. Le départage n'est pas
symétrique au critère principal — les sortantes ne coûtent rien au moment de l'extraction, elles
coûtent après — et c'est pourquoi elles passent en second.

**Le plan est complet et se recalcule, deux propriétés qui vont ensemble.** Le module central figure
au plan, en dernier — et ce rang est une information, pas une condamnation : chaque extraction
réussie retire des entrantes aux modules restants, et le monstre d'aujourd'hui sera abordable quand
son tour viendra, le système autour de lui s'étant vidé. C'est aussi pourquoi le plan se recalcule
après chaque extraction plutôt que de se suivre aveuglément : les comptes qui l'ont produit ne sont
plus vrais dès la première bascule. Le plan est une photographie ordonnée, pas un contrat — le même
statut que le podium des points chauds, recalculé chaque trimestre.

**Les refus habituels, pour la raison habituelle.** Des comptes négatifs ou un module en double
décrivent une analyse de dépendances corrompue, et un plan de migration bâti dessus engagerait des
mois de travail sur des données fausses — le refus coûte une minute, l'erreur coûterait un
trimestre.

En entretien, la stratégie se nomme strangler fig, et la question type est exactement celle de
l'ordre : « par quel module commencez-vous ? ». La réponse « le moins appelé, pour former l'équipe à
bas risque » distingue l'expérience du dogme.
