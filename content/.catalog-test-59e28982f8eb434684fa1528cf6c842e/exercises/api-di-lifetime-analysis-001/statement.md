# Recommander une durée de vie d'injection à partir du profil d'une dépendance

Implémentez `Submission.RecommendedLifetime` avec la signature fournie. Choisir la durée de vie d'un
service enregistré ne se fait pas au hasard ni à l'habitude : elle se déduit du profil de la
dépendance. Votre fonction lit ce profil et rend la recommandation motivée.

## Le format du profil

Le profil tient en trois paires clé-valeur séparées par des points-virgules, l'ordre n'important
pas :

- `state` — l'état que le service porte : `none`, `per-request` (des données propres à la requête en
  cours) ou `shared-mutable` (un état partagé et modifié, comme un cache applicatif) ;
- `cost` — le coût de construction : `cheap` ou `expensive` ;
- `uses-scoped` — le service consomme-t-il un service de requête, comme un contexte de données :
  `yes` ou `no`.

## La recommandation

Rendez `durée|raison`, en appliquant les règles dans cet ordre :

1. `state=shared-mutable` **et** `uses-scoped=yes` → `conflict|captive-dependency` : aucun choix de
   durée ne réconcilie un état partagé avec un service recréé à chaque requête — le profil est à
   corriger, pas à enregistrer ;
2. `state=per-request` → `scoped|request-state` ;
3. `state=shared-mutable` → `singleton|shared-state` ;
4. `uses-scoped=yes` → `scoped|scoped-dependency` : vivre plus longtemps figerait la première
   instance du service consommé — la dépendance captive ;
5. `cost=expensive` → `singleton|construction-cost` ;
6. sinon → `transient|stateless-cheap`.

```text
RecommendedLifetime("state=per-request;cost=cheap;uses-scoped=no")   →  "scoped|request-state"
RecommendedLifetime("state=none;cost=expensive;uses-scoped=no")      →  "singleton|construction-cost"
RecommendedLifetime("state=none;cost=cheap;uses-scoped=no")          →  "transient|stateless-cheap"
```

## Les refus

`ArgumentException` dès que le profil se décrit mal : paire illisible, clé répétée, attribut
absent, valeur hors vocabulaire.

## Avant d'écrire

Prédisez la recommandation pour un service sans état mais cher qui consomme un contexte de données,
et expliquez pourquoi le coût perd contre la consommation. Nommez ce que la dépendance captive
produirait en production : quelle instance, vue par qui, pendant combien de temps.
