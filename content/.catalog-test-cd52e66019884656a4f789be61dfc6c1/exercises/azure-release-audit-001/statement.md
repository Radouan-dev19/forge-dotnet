# Auditer le dossier de preuves d'un jalon de livraison

Implémentez `Submission.MilestoneVerdict` avec la signature fournie. Un jalon ne se déclare pas prêt,
il se prouve : le dossier rassemble des pièces, chacune datée par rapport au dernier changement du
code, et l'audit rend un verdict motivé. Votre fonction est cet audit.

## Le format du dossier

Des pièces `nom=état` séparées par des points-virgules. Le référentiel exige trois pièces, dans cet
ordre : `tests`, `security-review`, `rollback-plan`. Chaque pièce fournie porte un état :

- `fresh` — produite après le dernier changement du code ;
- `stale` — produite avant : elle décrit un code qui n'est plus celui qui part ;
- `missing` — déclarée explicitement manquante.

Une pièce exigée absente du dossier compte comme manquante.

## Le verdict

`ready` quand les trois pièces exigées sont fraîches ; sinon `blocked|` suivi des pièces fautives au
format `nom=état`, jointes par des points-virgules, **dans l'ordre du référentiel** — pas dans
l'ordre du dossier fourni.

```text
MilestoneVerdict("tests=fresh;security-review=fresh;rollback-plan=fresh")
  →  "ready"
MilestoneVerdict("tests=stale;security-review=fresh;rollback-plan=fresh")
  →  "blocked|tests=stale"
MilestoneVerdict("rollback-plan=fresh")
  →  "blocked|tests=missing;security-review=missing"
```

La distinction entre périmé et manquant n'est pas un luxe : la correction diffère. Une preuve
périmée se rejoue — le travail existe, il date ; une preuve absente se produit — le travail n'a
jamais eu lieu. Un verdict qui fond les deux fait perdre ce diagnostic.

## Les refus

`ArgumentException` pour un dossier vide, une pièce illisible, une pièce hors référentiel — un
dossier n'improvise pas ses pièces —, une pièce répétée ou un état hors vocabulaire.

## Avant d'écrire

Prédisez le verdict d'un dossier où les trois pièces sont périmées, puis d'un dossier qui fournit la
revue de sécurité seule. Dites ce que chacun des deux verdicts déclenche comme travail, et pourquoi
des tests verts d'avant le dernier correctif ne prouvent rien sur ce qui part.
