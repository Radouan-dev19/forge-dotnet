# Explication

Les décisions de découpage échouent des deux côtés : le monolithe qu'on n'ose pas toucher et la
constellation de services qu'on ne sait plus faire évoluer. La cascade de cet exercice n'invente
rien — elle ordonne ce que l'expérience collective a payé pour apprendre, et l'ordre est tout son
contenu.

**L'interdit technique se vérifie avant toute motivation, parce qu'il ne se négocie pas.** Deux
modules qui écrivent les mêmes données, ou qui participent à la même transaction, partagent des
invariants : le stock ne descend pas sous zéro, le débit correspond à une commande. À l'intérieur
d'un processus, une transaction locale les protège gratuitement. Découpés, ces invariants exigent le
répertoire complet de la piste senior — sagas, compensations, idempotence — c'est-à-dire des
semaines d'ingénierie et des modes de défaillance nouveaux, pour résoudre un problème qui était
organisationnel. La cascade répond donc à l'équipe pressée : découpler les **données** d'abord —
posséder chacun les siennes —, découper les services ensuite. L'inverse s'appelle un monolithe
distribué : tous les coûts du réseau, aucun des bénéfices de l'autonomie.

**La lecture seule ne verrouille pas, et cette nuance décide souvent.** Des données consultées sans
être modifiées se servent par réplication, par cache, par vue publiée : aucun invariant commun n'est
en jeu, seulement une fraîcheur à négocier. Classer la lecture avec l'écriture condamnerait au
maintien des modules parfaitement découpables — le catalogue que tout le monde lit et qu'un seul
écrit est l'exemple canonique.

**Les motivations se graduent, parce que la raison rendue pilote la revue future.** Équipe et
cadence différentes ensemble décrivent une évolution réellement indépendante — la raison la plus
solide. La cadence seule dit une pression de livraison : l'équipe est la même, mais un module doit
partir plus souvent que le reste. L'équipe seule dit une friction de propriété. Ces raisons ne sont
pas interchangeables : quand le profil change — les équipes fusionnent, la cadence s'aligne — c'est
la raison enregistrée qui dit si la décision mérite d'être rouverte.

**L'absence de force motrice conclut au maintien, et ce n'est pas un échec.** Le découpage n'est
jamais gratuit : un appel de méthode devient un appel réseau, avec sa latence, ses pannes partielles
et son observabilité à construire. Sans invariant à isoler ni pression organisationnelle, le
monolithe modulaire — des frontières nettes dans un seul déployable — offre la même lisibilité pour
une fraction du coût. La cascade encode ce que la mode oublie : le service est un moyen, la frontière
est le but.

En entretien, ce raisonnement s'adosse au vocabulaire des frontières — bounded context, couplage,
monolithe distribué — et la meilleure réponse commence toujours par la question des données : qui
écrit quoi, avec qui, dans quelle transaction.
