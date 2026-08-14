# Explication

Le retour de redirection est une requête entrante comme une autre — c'est toute sa fragilité —
et le `state` qu'elle porte est la seule chose qui la rattache à une demande réellement émise
par ce client. Cet exercice fait rendre le verdict complet, à quatre issues, et c'est la
taxonomie qui porte la valeur : chaque verdict raconte une histoire différente et déclenche une
réponse différente.

`accepted` est le cas nominal : le retour porte un `state` en attente — émis, jamais servi — et
le flux continue ; l'acceptation *consomme* l'entrée, ce que la fonction pure exprime par le
classement et que le code d'intégration réalise par un retrait atomique. `missing` dit qu'aucun
rattachement n'est possible : un retour sans `state` peut être un guichet mal configuré ou une
sonde — il se refuse sans même consulter les registres, d'où sa position en tête. `replayed`
dit qu'une adresse de rappel *déjà servie* revient : lien recliqué depuis un historique dans le
cas bénin, capture rejouée dans le cas hostile — et l'ambiguïté même justifie un verdict
distinct, car la réponse opérationnelle est « refuser et journaliser », pas « refuser en
silence ». `forged` dit que ce client n'a jamais émis ce `state` : c'est la signature de la
requête forgée inter-site en cours, le verdict le plus grave du lot.

L'ordre de classement est le contrat le plus subtil : le registre des consommés se consulte
*avant* celui des attentes, si bien qu'un `state` présent dans les deux — un registre mal purgé,
une course entre deux onglets — se classe rejeu. La règle se justifie par la prudence : entre
deux lectures possibles d'un même fait, un vérificateur de sécurité choisit la plus
défavorable, et le cas caché posé sur la double appartenance fige ce choix. L'erreur inverse —
consulter les attentes d'abord — transformerait précisément la situation ambiguë en acceptation.

La mécanique est du vocabulaire connu : découpage des registres en ensembles — segments rognés,
vides ignorés, l'hygiène des listes encodées —, appartenance ordinale sensible à la casse — un
`state` est un identifiant technique imprévisible, toute tolérance de casse élargirait l'espace
qu'un forgeur peut atteindre. Les registres absents valent vides : le client fraîchement
démarré n'a rien émis, et tout retour est alors forgé ou égaré, jamais accepté.

Le coût est linéaire dans la taille des registres, avec deux ensembles temporaires. La
transposition dépasse OAuth : jetons anti-falsification de formulaires, liens à usage unique,
codes de confirmation — partout où une réponse doit se rattacher à une demande, la même
taxonomie s'applique : absent, servi, attendu, inconnu — dans cet ordre de consultation, avec
le rejeu qui prime et des verdicts nommés pour que les journaux racontent l'attaque au lieu de
la fondre dans un refus générique.
