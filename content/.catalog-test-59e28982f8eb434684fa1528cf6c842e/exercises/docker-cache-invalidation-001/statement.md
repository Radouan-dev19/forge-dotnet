# Localiser la première couche invalidée par un fichier modifié

Implémentez `Submission.RebuiltSteps` avec la signature fournie. Vous recevez la description d'un
fichier de construction d'image, instruction par instruction, et le chemin du fichier qui vient de
changer dans le dépôt. Votre fonction dit combien d'instructions vont se reconstruire — c'est le prix,
en couches, de cette modification.

## Le format des instructions

Une liste séparée par des points-virgules, chaque instruction au format `verbe:détail` :

- `from:` — l'image de base, obligatoirement en première position ;
- `workdir:` — le répertoire de travail ;
- `copy:` — une copie depuis le dépôt : le détail est le chemin copié ;
- `run:` — une commande exécutée pendant la construction.

Une copie dont le détail se termine par une barre oblique couvre tout le répertoire ; sans barre
oblique finale, elle désigne un fichier exact.

## Ce qu'il faut produire

Le cache raisonne dans l'ordre du fichier : la **première** instruction de copie dont la portée
contient le chemin modifié est invalidée, et toutes les instructions qui la suivent se reconstruisent
avec elle. Rendez ce nombre d'instructions reconstruites ; rendez zéro quand aucune copie n'est
concernée — modifier un fichier jamais copié ne coûte aucune couche.

```text
RebuiltSteps("from:sdk-base;copy:project.csproj;run:restore;copy:src/;run:publish", "src/Program.cs")
  →  2
RebuiltSteps("from:sdk-base;copy:project.csproj;run:restore;copy:src/;run:publish", "project.csproj")
  →  4
```

Les deux exemples racontent la règle d'or des fichiers de construction : le manifeste de dépendances,
copié tôt, invalide presque tout quand il change — c'est accepté, il change rarement. Les sources,
copiées tard, n'invalident que la fin.

## Les refus

`ArgumentException` pour une liste vide, une instruction illisible ou de verbe inconnu, une première
instruction qui n'est pas `from:`, ou un chemin modifié vide.

## Avant d'écrire

Prédisez le coût d'une modification du fichier de dépendances si la copie des sources était placée
avant la restauration, et comparez-le à l'ordre de l'exemple. Dites ce que cette différence coûte à
chaque validation du dépôt.
