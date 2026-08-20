# Trier les assertions qui survivent à un remaniement

Implémentez `Submission.SurvivingAssertions` avec la signature fournie. Votre équipe prépare un
remaniement qui préserve le comportement — mêmes entrées, mêmes sorties, mêmes effets — mais
réorganise entièrement la mécanique interne. Avant de commencer, elle inventorie les assertions de la
suite existante pour savoir lesquelles resteront un filet et lesquelles casseront pour rien.

## Les natures d'assertion

Le flux d'entrée liste les natures des assertions, séparées par des points-virgules :

- `result` — la valeur rendue par l'opération ;
- `error-contract` — le type d'exception promis par le contrat ;
- `state` — l'état relisible après coup par l'API publique ;
- `calls` — le nombre d'appels à un collaborateur interne ;
- `order` — l'ordre des appels internes ;
- `private-state` — un champ privé lu par réflexion ;
- `timing` — la durée d'exécution.

## Ce qu'il faut produire

Rendez, dans l'ordre d'apparition et avec leurs répétitions, les natures qui **survivent** à un
remaniement préservant le comportement : celles qui observent le contrat, pas la mécanique. Joignez
avec des points-virgules ; quand rien ne survit, rendez la chaîne vide.

```text
SurvivingAssertions("result;calls;state")  →  "result;state"
SurvivingAssertions("order;timing")        →  ""
```

## La ligne de partage

Une assertion survit quand ce qu'elle observe est accessible à un appelant ordinaire : la valeur
rendue, l'exception promise, l'état relisible. Elle casse quand elle épie la façon dont le résultat
est obtenu — appels, ordre, champs privés, durée — car c'est précisément ce que le remaniement a le
droit de changer. Une suite dominée par la deuxième famille ne protège pas le comportement : elle
cimente l'implémentation.

## Les refus

`ArgumentException` pour un flux vide ou blanc, et pour toute nature qui ne figure pas dans la liste
ci-dessus — l'ignorer fausserait l'inventaire au moment exact où l'équipe s'y fie.

## Avant d'écrire

Prédisez la sortie pour un flux où chaque nature apparaît une fois, puis pour un flux qui répète
`result` deux fois. Dites pourquoi la répétition doit être conservée telle quelle.
