# Cadrer une architecture par le parcours critique

## Objectif observable

À la fin de cette leçon, vous saurez délimiter un projet par le parcours qui doit fonctionner de bout
en bout, justifier chaque découpage par une contrainte réelle, et écrire une décision d'architecture
qui reste lisible dans six mois.

## Prérequis

- Avoir lu `performance-security-incident-001` et savoir ce qu'un service doit rendre observable.
- Avoir lu `azure-hosting-choice-001` et savoir partir des contraintes.

## Intuition

Un projet ne se cadre pas par la liste de ses fonctionnalités : il se cadre par **le parcours qui doit
marcher de bout en bout**. Une commande créée, payée, expédiée, consultable. Ce fil traverse toutes
les couches et prouve que l'ensemble tient.

Tout le reste — les écrans secondaires, les cas particuliers, les optimisations — vient après, et
seulement si le parcours critique est solide. Cinq fonctionnalités à moitié faites ne valent pas un
parcours complet.

## Explication

**Écrire le parcours avant l'architecture.** Une phrase par étape, du déclencheur au résultat
observable. Cette liste sert ensuite de critère : toute décision technique se juge à ce qu'elle
apporte au parcours. Une couche qui ne sert aucune étape n'a pas de raison d'exister.

**Découper par responsabilité, pas par type de fichier.** Les couches classiques — domaine,
application, infrastructure, présentation — ne sont pas une cérémonie : chacune répond à une question
différente. Le domaine porte les règles qui tiennent quel que soit l'appelant, comme dans
`tests-domain-rules-001`. L'application orchestre un cas d'usage. L'infrastructure parle au monde
extérieur. La présentation traduit, comme le contrôleur mince de `api-controllers-dtos-001`.

Le critère de vérification est simple : les dépendances pointent vers le domaine, jamais l'inverse. Si
le domaine connaît la base de données, le découpage est décoratif.

**Le nombre de composants est une décision coûteuse.** Découper en plusieurs services déployables
séparément ajoute du réseau, de la latence, des pannes partielles, une observation distribuée et des
déploiements coordonnés. Ce coût se justifie par une contrainte réelle — des équipes séparées, des
profils de charge très différents, une exigence d'isolation — jamais par la propreté supposée.

Pour un projet mené seul, un seul déployable bien découpé à l'intérieur est presque toujours le bon
choix, et il reste séparable plus tard.

**Ce qui appartient au projet et ce qui n'y appartient pas.** Un périmètre s'écrit en deux colonnes.
La seconde — ce qui est explicitement hors périmètre — est celle qui protège : sans elle, chaque
discussion rouvre le sujet, et le projet ne se termine jamais.

**Les décisions se consignent au moment où elles sont prises.** Le format utile tient en cinq
sections : le contexte, la décision, les options écartées avec leur critère d'exclusion, les
conséquences acceptées, et la condition qui justifierait de revenir dessus. C'est le même besoin que
la trace de choix d'hébergement, généralisé.

Ce qui coûte le plus cher n'est pas une mauvaise décision : c'est une décision dont personne ne se
souvient de la raison, que l'on n'ose donc ni garder ni changer.

**Les contraintes transversales se décident tôt.** Authentification, autorisation, forme des erreurs,
journalisation, migrations. Les ajouter après coup demande de repasser sur tout le code. Les poser
d'emblée coûte quelques heures et évite des semaines — c'est vrai en particulier de l'autorisation au
niveau de la ressource, vue dans `security-authorization-roles-policies-001`.

**Un risque identifié tôt se traite ; un risque découvert tard se subit.** Lister les trois plus gros
risques du projet et, pour chacun, ce qu'on fait pour le réduire dès la première semaine. Le risque
le plus fréquent est une dépendance externe dont on n'a jamais vérifié qu'elle répond comme on le
croit.

## Exemple commenté

Le parcours critique, écrit avant toute décision technique :

```text
Parcours critique — service de commandes

1. Un client authentifié crée une commande avec au moins une ligne.
2. Le stock est vérifié ; une commande impossible est refusée avec un motif exploitable.
3. La commande est persistée et reçoit un identifiant.
4. Le client reçoit une confirmation portant cet identifiant.
5. Le client consulte sa commande, et uniquement les siennes.
6. Un opérateur marque la commande expédiée.

Hors périmètre pour cette version
  - paiement en ligne (simulé par un état)
  - retours et avoirs
  - notifications par courriel
  - interface d'administration au-delà de l'étape 6
```

Le découpage, avec le sens des dépendances :

```text
Presentation   ->  Application  ->  Domain  <-  Infrastructure

Domain          règles de commande, invariants, aucun accès extérieur
Application     cas d'usage « créer une commande », orchestration, transactions
Infrastructure  persistance, appels sortants — implémente les abstractions du domaine
Presentation    points d'entrée HTTP, DTO, traduction des résultats en statuts

Un seul déployable : équipe d'une personne, profil de charge unique,
aucune exigence d'isolation. Séparable plus tard si l'une de ces trois
conditions change.
```

Et une décision consignée, dans sa forme complète :

```text
Décision 003 — persistance relationnelle avec correspondance objet

Contexte    Entités reliées (commande, ligne, client), invariants transversaux,
            besoin d'agrégats pour la consultation. Développeur seul, quatre semaines.

Décision    Base relationnelle, correspondance objet, migrations versionnées.

Écarté      Stockage documentaire — les invariants entre commande et lignes devraient
            être maintenus dans le code, sans garantie du moteur.
            Requêtes écrites à la main partout — coût de maintenance disproportionné
            pour un domaine de cette taille.

Conséquences acceptées
            Requêtes générées à surveiller sur les listes ; risque d'accès en boucle,
            traité par des projections explicites et un test de nombre de requêtes.

Réviser si  le volume dépasse ce qu'une instance unique sert confortablement, ou si
            une partie du domaine devient réellement sans schéma.
```

## Contre-exemple et erreur fréquente

```text
Architecture proposée — semaine 1

  7 services déployables, chacun avec sa base
  1 bus de messages, 1 passerelle d'interface, 1 registre de services
  1 cache distribué, 1 moteur de recherche
  Communication par événements entre tous les services

Équipe : 1 personne. Durée : 4 semaines. Utilisateurs attendus : 30.
Parcours critique : non écrit.
Périmètre : « toutes les fonctionnalités d'un système de gestion ».
Décisions consignées : aucune.
```

Cinq défauts.

Le parcours critique n'est pas écrit. Rien ne permet donc de juger si un composant sert à quelque
chose, et la première démonstration de bout en bout arrivera très tard — souvent trop tard pour
corriger.

Sept services déployables pour une personne signifient sept déploiements, sept jeux de journaux, sept
sources de pannes partielles, et une observation distribuée à construire avant même la première
fonctionnalité. Le coût est immédiat, le bénéfice hypothétique.

Chaque service ayant sa base, les invariants entre commande et ligne ne sont plus garantis par un
moteur : ils deviennent du code à écrire, à tester et à maintenir.

Le périmètre non borné garantit que le projet ne se terminera pas. Sans colonne « hors périmètre »,
chaque discussion en ajoute.

Enfin, aucune décision n'est consignée. Dans trois semaines, personne ne saura pourquoi il y a un bus
de messages, et il restera parce que le retirer paraîtra risqué.

## Vérification de compréhension

Vous disposez de quatre semaines seul pour un service de réservation. Écrivez le parcours critique en
cinq étapes, et nommez deux éléments que vous placez explicitement hors périmètre.

:::quiz
id=final-project-architecture-001-check
question=Pourquoi écrire le parcours critique avant de décider de l'architecture ?
option=Parce que le parcours détermine automatiquement le nombre de couches nécessaires
option=Parce qu'il devient le critère de jugement : une décision technique se justifie par ce qu'elle apporte à une étape du parcours, et un composant qui n'en sert aucune n'a pas lieu d'être
option=Parce que les utilisateurs valident le parcours avant que le code ne commence
correct=1
success=Correct : sans ce critère, les décisions se justifient par le goût ou l'habitude, et la première démonstration de bout en bout arrive trop tard.
retry=Relisez le passage sur le parcours critique, et demandez-vous à quoi se compare une décision technique en son absence.
:::

## Exercice guidé

Cette leçon se pratique sur le projet final, dont le cahier des charges se trouve dans
`content/reference/projects/`.

1. Lisez le cahier des charges et écrivez le parcours critique en six étapes au plus, du déclencheur au
   résultat observable.
2. Écrivez la colonne « hors périmètre » : au moins quatre éléments, chacun avec sa raison.
3. Dessinez le découpage en couches et vérifiez le sens des dépendances : rien ne doit pointer vers
   l'extérieur depuis le domaine.
4. Consignez vos trois premières décisions au format complet — contexte, décision, options écartées,
   conséquences, condition de révision.

## Exercice autonome

Cadrez un second projet, différent du projet final : un suivi d'interventions, un gestionnaire de
prêts, ou un outil de votre choix.

Produisez : le parcours critique, le périmètre en deux colonnes, le découpage avec le sens des
dépendances, le nombre de déployables avec sa justification, les contraintes transversales décidées
d'emblée, les trois principaux risques et leur traitement en première semaine, et cinq décisions
consignées.

## Débogage

Un ticket indique : « Le projet a trois semaines de retard et aucune démonstration de bout en bout n'a
encore eu lieu. »

1. **Symptôme** : beaucoup de code écrit, rien de démontrable.
2. **Hypothèse** : le travail a suivi les couches plutôt que le parcours — tout le domaine, puis toute
   l'infrastructure — et rien ne se rejoint.
3. **Preuve** : essayez d'exécuter la première étape du parcours de bout en bout. L'impossibilité
   confirme.
4. **Prévention** : construire une tranche verticale complète en premier, même minimale, puis élargir.

## Entretien

Question posée à voix haute : *comment cadrez-vous un projet dont vous êtes seul responsable ?*

Une réponse solide part du parcours critique, borne le périmètre par ce qui en est exclu, justifie le
nombre de déployables par une contrainte réelle, décide les préoccupations transversales d'emblée, et
cite la consignation des décisions comme protection contre l'oubli.

## Résumé

- Le parcours critique s'écrit avant l'architecture et sert de critère de jugement.
- Les dépendances pointent vers le domaine ; l'inverse rend le découpage décoratif.
- Chaque déployable supplémentaire a un coût immédiat et un bénéfice hypothétique.
- La colonne « hors périmètre » est celle qui permet de finir.
- Une décision non consignée devient intouchable faute de raison connue.

## Cartes de révision

Question : quel est le premier livrable utile d'un projet ? Réponse attendue : une tranche verticale
complète, même minimale, qui traverse toutes les couches.

Question : quelles préoccupations décider dès le départ ? Réponse attendue : authentification,
autorisation, forme des erreurs, journalisation, migrations — les ajouter après demande de repasser
partout.

## Test de maîtrise

Sans relire, cadrez entièrement un projet de quatre semaines : parcours critique, périmètre en deux
colonnes, découpage et sens des dépendances, nombre de déployables avec justification, contraintes
transversales, trois risques et leur traitement, et trois décisions consignées au format complet.

Cette auto-évaluation ne crée aucune preuve de maîtrise.
