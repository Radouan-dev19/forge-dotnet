# Explication

Cet exercice isole la décision la plus contre-intuitive de la validation d'un jeton : lire une
information dans l'en-tête pour aussitôt refuser de lui obéir. Le paradoxe n'est qu'apparent, et le
comprendre vaut mieux que mémoriser la règle.

L'en-tête d'un jeton est écrit par celui qui fabrique le jeton. Dans le cas nominal, c'est
l'émetteur légitime ; dans le cas qui intéresse la sécurité, c'est un attaquant. Or l'annonce
`alg` décrit le mécanisme par lequel le jeton devrait être jugé. Laisser cette annonce piloter la
vérification revient donc à laisser l'accusé choisir son tribunal. L'histoire du format a montré
les deux exploitations classiques. La première est brutale : annoncer `none`, l'algorithme « sans
signature » que la norme prévoyait pour des usages internes, et présenter un jeton nu — des
bibliothèques entières l'ont accepté. La seconde est plus fine : face à un serveur qui vérifie une
signature asymétrique, annoncer un HMAC et signer avec la clé *publique* du serveur, que tout le
monde connaît ; une implémentation qui choisit le mécanisme d'après l'en-tête vérifie alors un HMAC
parfaitement valide, construit sans aucun secret.

La parade tient dans le sens de la comparaison. Le vérificateur n'utilise pas l'annonce : il la
*confronte* à ce qu'il a lui-même décidé, et le moindre écart est un rejet. C'est pourquoi la
méthode reçoit l'algorithme exigé en paramètre et répond par un booléen : elle matérialise cette
confrontation, rien d'autre. Notez la double règle de casse, qui piège en entretien comme en
revue : la confrontation avec l'algorithme exigé est sensible à la casse — `hs256` n'est pas
`HS256`, la norme des en-têtes JOSE est stricte —, mais le refus de `none` est insensible à la
casse, parce qu'il s'agit d'une liste noire et qu'une liste noire laxiste ne bloque rien. Un refus
strict de la seule forme minuscule laisserait passer `None`, et l'attaque avec lui.

Le refus de `none` est par ailleurs inconditionnel : même si l'appelant exigeait `none`, la méthode
répond faux. On peut discuter ce choix — après tout, l'appelant est du code de confiance — mais il
suit le principe des mécanismes de sécurité : rendre l'état dangereux impossible à exprimer plutôt
que compter sur la discipline de chaque appelant. Une configuration qui voudrait désactiver la
signature devra le faire ailleurs, visiblement, pas en passant une chaîne magique.

Enfin, le régime d'erreurs est celui de tout vérificateur : un en-tête indécodable, un JSON qui
n'est pas un objet, une annonce manquante sont des refus silencieux, pas des exceptions. La méthode
trie ce que le réseau apporte ; elle ne s'étonne de rien.
