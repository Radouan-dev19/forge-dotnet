# Journal d'exploitation exploitable

Trois méthodes qui produisent un journal à travers le **vrai `ILogger` de
`Microsoft.Extensions.Logging`**, disponible dans le bac à sable. Le puits fourni capture ce que
votre code émet réellement : niveaux, portées et texte final. C'est lui qui rend le résultat — si
vous fabriquez la chaîne à la main sans passer par le journal, le seuil et les portées ne
s'appliqueront pas et vos cas échoueront.

## Ce qui vous est fourni

Le squelette contient `MemoryLog` — un `ILogger` complet avec seuil, portées empilées et rendu
d'entrées —, `ParseLevel` pour convertir le seuil demandé, et `Render` pour restituer les entrées
capturées. **Ne les modifiez pas** : leur format d'entrée est le contrat des cas.

Une entrée capturée a la forme `NIV portées message`, où `NIV` vaut `DBG`, `INF`, `WRN` ou `ERR`,
et où les portées ouvertes sont jointes par `>` puis suivies d'une espace — rien quand aucune
portée n'est ouverte. `Render` joint les entrées par `|` et rend `(vide)` sans entrée.

Vous pouvez ajouter jusqu'à trois fichiers à côté du rendu.

## Le contrat

```csharp
public static string JournalRun(string steps, string minimumLevel);
public static string SecureEntry(string message, string secrets);
public static string CorrelatedJournal(string requestId, string steps);
```

### `JournalRun`

`steps` décrit une exécution : des éléments `etape:statut` ou `etape:statut:detail` joints par `;`.
La nature du statut impose le niveau et le message :

| Statut | Niveau | Message émis |
|---|---|---|
| `ok` | Information | `{etape} terminé` |
| `reprise` | Warning | `{etape} rejoué` |
| `echec` | Error | `{etape} en échec : {detail}` |

`minimumLevel` — `debug`, `information`, `warning` ou `error` — configure le seuil du puits via
`ParseLevel`. Rendez `Render` du puits après avoir émis chaque étape dans l'ordre.

### `SecureEntry`

Émet `message` en Information, après avoir remplacé par `***` **chaque occurrence de chaque
secret** — les secrets sont joints par `;`, la liste peut être vide. Le remplacement se fait avant
l'émission : un secret qui atteint le puits est déjà une fuite, même si l'affichage le masque
ensuite. L'occurrence en sous-chaîne compte aussi : `port` se caviarde dans `rapport`.

### `CorrelatedJournal`

Ouvre la portée `cid={requestId}` avec `BeginScope`, puis émet chaque étape de `steps` (jointes par
`;`) en Information. Une étape `groupe/action` ouvre la portée imbriquée `{groupe}` le temps
d'émettre `{action}`, puis la referme. L'identifiant ne doit apparaître dans aucun message : c'est
la portée qui le porte, et c'est ce qui rend la corrélation gratuite pour les étapes suivantes.

## Ce qui est mesuré

Trois suites d'acceptation, une par jalon, exécutées dans le bac à sable. Les trois doivent être
vertes pour que le projet compte comme livrable vérifié — il satisfait alors l'exigence
**journaux exploitables** de la porte C.

## Ce qui n'est pas mesuré

Le branchement d'un fournisseur réel — console, fichier, agrégateur — et la politique de rétention.
La grille les observe ; sachez dire ce que votre seuil et votre caviardage deviendraient en
production.
