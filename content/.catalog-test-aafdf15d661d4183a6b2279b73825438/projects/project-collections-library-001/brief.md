# Bibliothèque de collections

Trois transformations sur une liste de valeurs séparées par des points-virgules. Aucune ne modifie
son entrée, et aucune ne dépend de l'ordre dans lequel un dictionnaire rend ses clés.

## Contrat

Le rendu déclare `public static class Submission` et expose exactement ces trois méthodes.

```csharp
public static string Normalize(string values);
public static Dictionary<string, int> Frequencies(string values);
public static string TopValues(string values, int count);
```

### Assainissement — la règle commune aux trois

Une entrée est découpée sur le point-virgule. Chaque segment perd ses blancs de bordure ; un segment
devenu vide est écarté. Ce qui reste est une **valeur**. La comparaison entre valeurs est ordinale :
`Paris` et `paris` sont deux valeurs différentes.

Une entrée absente est un appel fautif et lève `ArgumentNullException`. Une entrée vide, ou qui ne
contient aucune valeur utile, n'est pas fautive : elle produit un résultat vide.

### `Normalize`

Rend les valeurs débarrassées de leurs répétitions, jointes par un point-virgule sans espace. L'ordre
est celui de la **première apparition** — pas l'ordre alphabétique.

```text
"  bleu ; vert;bleu ;; rouge "  ->  "bleu;vert;rouge"
```

### `Frequencies`

Rend le nombre d'occurrences de chaque valeur. Le comptage porte sur les valeurs assainies, donc
`" bleu"` et `"bleu"` alimentent la même entrée.

```text
"bleu; bleu ;vert"  ->  { "bleu": 2, "vert": 1 }
```

### `TopValues`

Rend les `count` valeurs les plus fréquentes, jointes par un point-virgule. À fréquence égale, la
valeur qui vient en premier dans l'ordre **ordinal croissant** sort en premier — c'est ce qui rend le
résultat reproductible d'une exécution à l'autre.

Une demande nulle ou négative rend une chaîne vide. Une demande supérieure au nombre de valeurs
distinctes rend toutes les valeurs, sans complément ni refus.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les cas visibles vous
montrent leurs échecs ; les cas cachés restent côté serveur. Les trois suites doivent être vertes
pour que le projet compte comme livrable vérifié.

Vous pouvez ajouter jusqu'à trois fichiers de votre choix à côté du rendu — un type d'aide, une
méthode d'extension — tant que `Submission` expose les trois méthodes ci-dessus.

## Ce qui n'est pas mesuré, et qui compte quand même

Le journal du défaut que vous avez reproduit puis corrigé, et la façon dont vous nommez la limite de
votre solution. La grille les observe ; aucune suite ne peut les vérifier à votre place.
