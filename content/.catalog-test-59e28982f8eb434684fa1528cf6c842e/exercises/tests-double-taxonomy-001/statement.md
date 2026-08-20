# Attribuer son double à chaque dépendance d'un test

Implémentez `Submission.DoublesFor` avec la signature fournie. Avant d'écrire un test, une équipe
inventorie les dépendances du code visé et décide, pour chacune, du double qui la remplacera. Votre
fonction exécute cette taxonomie : chaque dépendance décrite reçoit son double, et les descriptifs
incohérents sont refusés.

## Le format du descriptif

Des dépendances séparées par des points-virgules, chacune au format `nom:flux:contrat` :

- `flux` — `incoming` (la dépendance fournit des données au code) ou `outgoing` (le code lui envoie
  des effets) ;
- `contrat` — ce que le test doit honorer : `canned` (une réponse toute faite suffit),
  `behavioural` (le contrat de comportement compte : cohérence entre écritures et lectures),
  `state` (l'effet doit être relisible après coup) ou `protocol` (l'échange d'appels est le contrat,
  comme une séquence d'annulation).

## L'attribution

- `incoming` + `canned` → `stub` : servir la réponse, rien d'autre ;
- `incoming` + `behavioural` → `fake` : une implémentation légère qui respecte le contrat, comme un
  dépôt en mémoire ;
- `outgoing` + `state` → `fake` : l'effet s'accumule dans un état relisible, et le test lit l'état ;
- `outgoing` + `protocol` → `spy` : l'échange s'enregistre et le test vérifie le protocole.

Les deux croisements restants sont **refusés** avec `ArgumentException` : vérifier le protocole d'un
flux entrant cimente le nombre de lectures — de la sur-spécification que tout remaniement casse — et
demander une réponse toute faite à un flux sortant ne vérifie rien du tout.

Rendez `nom=double` joint par des points-virgules, dans l'ordre du descriptif.

```text
DoublesFor("clock:incoming:canned")                              →  "clock=stub"
DoublesFor("repository:incoming:behavioural;mailer:outgoing:protocol")
                                                                 →  "repository=fake;mailer=spy"
```

## Les refus

`ArgumentException` aussi pour un descriptif vide, une entrée sans ses trois champs, un champ hors
vocabulaire ou un nom de dépendance répété — deux doubles pour la même dépendance ne cohabitent pas
dans un test.

## Avant d'écrire

Classez de tête un journal d'audit dont le test relit les entrées, puis un bus de messages dont le
test vérifie l'ordre d'émission. Dites ce que chacun perdrait si on lui attribuait le double de
l'autre.
