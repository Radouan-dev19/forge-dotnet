# Rendre le verdict d'admission d'un cloisonnement

Implémentez `Submission.BulkheadVerdict` avec la signature fournie. Le cloisonnement borne ce qu'une
dépendance peut coûter au processus : un nombre maximal d'exécutions simultanées, une file d'attente
bornée, et un rejet rapide au-delà. Votre fonction rend le verdict d'admission d'une requête à partir
de l'état du cloisonnement.

## Les quatre nombres

La fonction reçoit la capacité d'exécution, le nombre d'exécutions en cours, la capacité de la file
et le nombre de requêtes en file.

## Le verdict

- un emplacement d'exécution est libre — en cours strictement sous la capacité —
  → `execute|slot-available` ;
- sinon, la file a de la place → `enqueue|slots-full` ;
- sinon → `reject|bulkhead-saturated` : le rejet rapide est la réponse saine, celle qui rend la main
  en microsecondes au lieu de retenir un fil d'exécution.

```text
BulkheadVerdict(10, 4, 20, 0)    →  "execute|slot-available"
BulkheadVerdict(10, 10, 20, 5)   →  "enqueue|slots-full"
BulkheadVerdict(10, 10, 20, 20)  →  "reject|bulkhead-saturated"
```

Une file de capacité **nulle** est un cloisonnement légitime — le plus strict : tout se joue sur les
emplacements d'exécution, et le refus est immédiat. Une capacité d'exécution nulle, elle, ne décrit
aucun cloisonnement : c'est un service condamné par configuration.

## Les refus

`ArgumentOutOfRangeException` pour une capacité d'exécution non positive ou une capacité de file
négative — la configuration est absurde. `ArgumentException` pour une occupation négative ou
supérieure à sa capacité : un tel relevé ne décrit aucun instant réel du cloisonnement, et rendre un
verdict dessus donnerait une décision plausible sur un état impossible.

## Avant d'écrire

Prédisez le verdict quand exécution et file sont exactement pleines, puis quand la file est pleine
mais qu'un emplacement d'exécution vient de se libérer. Dites ce que le rejet rapide protège
exactement — et pourquoi le convertir en attente illimitée reproduirait la panne que la cloison
devait contenir.
