# Analyseur de journaux

Mille lignes de journal, trente incidents, et une seule question utile : **lesquels sont le même
défaut vu trente fois ?** C'est la normalisation du message qui répond, pas le comptage.

## Contrat

Le rendu déclare `public static class Submission` et expose exactement ces trois méthodes.

```csharp
public static int CountBySeverity(string logs, string severity);
public static Dictionary<string, int> GroupByMessage(string logs);
public static string ErrorReport(string logs);
```

### Ce qu'est une entrée de journal

Les lignes sont séparées par un saut de ligne ; un retour chariot de fin de ligne est retiré avant
tout traitement. Une ligne est une **entrée** quand elle porte au moins trois mots séparés par des
blancs et que le **deuxième** est un niveau connu : `INFO`, `WARN` ou `ERROR`, en capitales.

```text
2026-08-11T09:15:04 ERROR Timeout apres 1200 ms
```

Le **message** est tout ce qui suit le niveau, blancs de bordure retirés. Toute ligne qui n'est pas
une entrée est ignorée sans bruit : un journal tronqué s'analyse quand même.

`logs` absent lève `ArgumentNullException`. Un `severity` absent ou fait de blancs lève
`ArgumentException` : demander « rien » n'est pas une question.

### `CountBySeverity`

Le nombre d'entrées portant ce niveau. La comparaison ignore la casse du niveau demandé : `error` et
`ERROR` posent la même question. Un niveau inconnu rend zéro — c'est une réponse, pas une faute.

### `GroupByMessage`

Ne regroupe que les entrées `ERROR`. Chaque message est d'abord **normalisé** : toute suite de
chiffres consécutifs devient un seul `#`.

```text
Timeout apres 1200 ms   ->  Timeout apres # ms
Timeout apres 900 ms    ->  Timeout apres # ms
```

Les deux comptent donc pour une seule panne, vue deux fois. Un message sans chiffre traverse la
normalisation inchangé. Le résultat associe chaque message normalisé à son nombre d'occurrences ;
aucun `ERROR` rend un dictionnaire vide.

### `ErrorReport`

Une ligne par message normalisé, la plus fréquente en tête, jointes par un saut de ligne :

```text
2 x Timeout apres # ms
1 x Disque plein
```

À fréquence égale, l'ordre **ordinal croissant** du message tranche — c'est ce qui rend deux
exécutions comparables. Aucun `ERROR` rend une chaîne vide.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les cas visibles vous
montrent leurs échecs ; les cas cachés restent côté serveur. Les trois doivent être vertes pour que
le projet compte comme livrable vérifié.

## Ce qui n'est pas mesuré

Le choix des variations que vous confondez volontairement. Remplacer les nombres est une décision :
elle réunit deux délais différents sous une même panne. Dites dans votre journal ce que cette
décision vous fait perdre.
