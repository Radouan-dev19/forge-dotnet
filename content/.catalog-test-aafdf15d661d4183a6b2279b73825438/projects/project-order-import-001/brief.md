# Import de commandes CSV vers JSON

Un fichier de commandes arrive sous forme de texte. Il faut en tirer trois choses : combien de
lignes sont exploitables, pourquoi les autres ne le sont pas, et quel total elles représentent.

## Contrat

Le rendu déclare `public static class Submission` et expose exactement ces trois méthodes.

```csharp
public static int CountValidRows(string csv);
public static string RejectionReport(string csv);
public static string ImportReport(string csv);
```

### Le format reçu

Les lignes sont séparées par un saut de ligne. Une fin de ligne Windows ne doit pas changer le
résultat : le retour chariot en fin de ligne est retiré avant tout traitement.

La **première ligne est l'en-tête**. Elle n'est jamais une donnée, quelle que soit sa forme. À partir
de la deuxième, une ligne vide ou faite de blancs est simplement ignorée : elle n'est ni valide ni
rejetée.

Une ligne de données porte trois champs séparés par un point-virgule :

```text
reference;quantite;prix
FR-1042;2;19.90
```

Elle est **valide** quand les quatre conditions tiennent :

| Champ | Condition |
|---|---|
| nombre de champs | exactement trois |
| `reference` | non vide une fois les blancs de bordure retirés |
| `quantite` | entier strictement positif |
| `prix` | décimal supérieur ou égal à zéro |

Les nombres sont lus en **culture invariante** : le séparateur décimal est le point. C'est ce qui
rend le résultat identique sur toutes les machines.

Une entrée absente lève `ArgumentNullException`. Un texte vide n'est pas fautif : il ne contient
aucune donnée.

### `CountValidRows`

Le nombre de lignes valides.

### `RejectionReport`

Une ligne par ligne rejetée, dans l'ordre du fichier, jointes par un saut de ligne :

```text
ligne 3 : quantite
ligne 5 : champs
```

Le numéro est celui de la ligne **dans le fichier reçu**, l'en-tête comptant pour la ligne 1. Le
motif est le **premier applicable** dans cet ordre : `champs`, `reference`, `quantite`, `prix`. Une
ligne qui cumule deux défauts n'en rapporte donc qu'un.

Aucun rejet rend une chaîne vide.

### `ImportReport`

Un objet JSON compact, clés dans cet ordre exact :

```text
{"valides":2,"rejetees":1,"total":41.30}
```

`total` est la somme des `quantite × prix` des seules lignes valides, **arrondie une seule fois à la
fin**, à deux décimales, les demis s'éloignant de zéro. Il s'écrit toujours avec ses deux décimales,
même quand elles sont nulles.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les cas visibles vous
montrent leurs échecs ; les cas cachés restent côté serveur. Les trois doivent être vertes pour que
le projet compte comme livrable vérifié.

## Ce qui n'est pas mesuré

La façon dont vous lisez le fichier depuis le disque, et ce que vous faites d'un fichier trop gros
pour la mémoire. La grille les observe ; posez-vous la question avant qu'on vous la pose.
