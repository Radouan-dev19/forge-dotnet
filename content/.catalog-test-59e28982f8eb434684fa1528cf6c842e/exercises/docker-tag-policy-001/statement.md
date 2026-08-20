# Contrôler qu'un manifeste de déploiement ne cite que des images épinglées

Implémentez `Submission.RejectedReferences` avec la signature fournie. Votre équipe applique une
politique d'immuabilité : une image ne se déploie que par son **empreinte**, parce qu'une étiquette —
même une version complète — peut être republiée et désigner demain un autre contenu qu'aujourd'hui.
Votre fonction contrôle un manifeste et liste les références qui violent la politique.

## Le format d'une référence

`nom[:étiquette][@sha256:empreinte]`, où le nom peut contenir un registre avec port, comme
`registry.local:5000/app`. Une empreinte valide s'écrit `sha256:` suivi d'exactement soixante-quatre
caractères hexadécimaux **minuscules**.

## Ce qu'il faut produire

Pour chaque référence refusée, dans l'ordre du manifeste, une entrée `référence=raison` jointe par des
points-virgules. Trois raisons existent :

- `untagged` — ni étiquette ni empreinte : l'implicite pointe vers l'étiquette flottante par défaut ;
- `mutable-tag` — une étiquette mais pas d'empreinte, y compris une version complète ;
- `invalid-digest` — une empreinte présente mais mal formée : trop courte, majuscules, alphabet hors
  hexadécimal.

Une référence portant une empreinte valide est acceptée, avec ou sans étiquette d'accompagnement.
Quand tout le manifeste est conforme, rendez la chaîne vide.

```text
RejectedReferences("cache:latest")  →  "cache:latest=mutable-tag"
```

## Le piège du port

Dans `registry.local:5000/app`, le deux-points appartient au port du registre, pas à une étiquette :
cette référence est `untagged`. La distinction se joue sur la position — une étiquette ne peut suivre
qu'après la dernière barre oblique.

## Les refus

`ArgumentException` pour un manifeste vide ou une entrée vide entre deux points-virgules — un trou
dans un manifeste n'est pas une image conforme, c'est un fichier corrompu.

## Avant d'écrire

Classez de tête : une image nue, une version complète, une version accompagnée d'une empreinte
valide, et un registre à port sans étiquette. Dites laquelle des quatre est la seule déployable sous
la politique, et pourquoi la version complète n'en fait pas partie.
