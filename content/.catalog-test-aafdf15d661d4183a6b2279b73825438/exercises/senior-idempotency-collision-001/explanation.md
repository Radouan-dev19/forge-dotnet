# Explication

L'idempotence par clé est un contrat à deux faces, et la plupart des équipes n'en testent qu'une. La
face connue : une opération relancée avec sa clé ne s'applique qu'une fois — le serveur reconnaît la
clé et rejoue la réponse mémorisée. La face oubliée : ce mécanisme suppose qu'une clé désigne une
seule opération, et rien, côté serveur, ne force un client à respecter cette hypothèse. Quand elle
casse, le symptôme est le plus discret de tout le répertoire des pannes distribuées : le second
paiement ne produit ni erreur, ni trace, ni effet — le serveur répond poliment la confirmation du
premier, et le client repart convaincu d'avoir payé.

**Pourquoi la collision vient presque toujours du générateur de clés.** Les clients fabriquent leurs
clés depuis ce qu'ils ont sous la main : un horodatage à la seconde, un identifiant d'écran, un
compteur remis à zéro au redémarrage. Chacune de ces sources finit par produire deux fois la même
clé pour deux opérations différentes — deux paiements dans la même seconde suffisent. L'audit du
journal est le seul endroit où cette famille de bogues se voit, parce que ni le premier client ni le
second ne reçoivent d'erreur : seul le rapprochement des empreintes sous une même clé trahit le
problème.

**Pourquoi la référence est la première empreinte, jamais la précédente.** Comparer chaque requête à
la précédente de sa clé raterait le scénario le plus courant : une opération légitimement relancée
deux fois — même empreinte —, puis une clé recyclée pour autre chose. La chaîne « h1, h1, h3 »
comparée de proche en proche montre une seule rupture, la même que « h1, h3 » ; mais si le journal
arrive fragmenté et que le fragment commence à « h1, h3 », la comparaison de proche en proche et la
comparaison à la référence divergent. L'empreinte de référence — celle que le serveur a mémorisée
avec la réponse — est ce que le mécanisme réel compare : l'audit doit juger comme lui.

**Pourquoi la relance légitime doit rester silencieuse.** Un audit qui signalerait toute clé répétée
serait inutilisable : les relances sont l'usage nominal du mécanisme, elles se produisent à chaque
incident réseau, et le rapport se noierait dans le normal. Pire, une équipe pressée « corrigerait »
en rendant les clés uniques par requête — détruisant l'idempotence pour faire taire l'audit. La
précision du verdict protège le mécanisme autant que les données.

**Le rapport est trié parce qu'il se compare.** Les clés en collision sortent une fois chacune, en
ordre ordinal : deux audits du même journal se comparent au caractère près, et le différentiel entre
deux campagnes montre les collisions apparues. En production, la parade est connue : le serveur qui
mémorise l'empreinte avec la réponse refuse la requête dont l'empreinte diffère — un conflit
explicite vaut infiniment mieux qu'un succès menteur. Cet exercice construit exactement le jugement
que ce refus exécute.
