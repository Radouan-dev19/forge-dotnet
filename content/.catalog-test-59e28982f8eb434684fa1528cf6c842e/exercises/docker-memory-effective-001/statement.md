# Résoudre la limite mémoire effective d'une chaîne de contraintes

Implémentez `Submission.EffectiveMemoryLimit` avec la signature fournie. Un conteneur ne reçoit pas
« sa » limite mémoire : il reçoit la plus contraignante d'une chaîne de plafonds posés à des étages
différents, et le diagnostic d'un conteneur tué par manque de mémoire commence par savoir lequel a
réellement agi.

## Le format de la chaîne

Des paires `source=mébioctets` séparées par des points-virgules. Les sources possibles, de la plus
lointaine à la plus proche du conteneur :

- `daemon` — la configuration par défaut du démon ;
- `cgroup-parent` — le groupe de contrôle parent imposé par la plateforme ;
- `compose` — la limite du fichier de composition ;
- `run` — l'option passée au lancement.

Chaque source apparaît au plus une fois ; une source absente n'impose rien.

## Ce qu'il faut produire

La valeur du plafond effectif — **le plus bas de la chaîne** — suivie d'une barre verticale et de la
source qui le porte. Quand plusieurs sources portent ce minimum, rapportez la plus proche du
conteneur : c'est elle que l'exploitant peut ajuster le plus vite. Une chaîne vide rend `unlimited`.

```text
EffectiveMemoryLimit("daemon=8192;compose=1024;run=512")  →  "512|run"
EffectiveMemoryLimit("compose=2048;cgroup-parent=1024")   →  "1024|cgroup-parent"
EffectiveMemoryLimit("")                                  →  "unlimited"
```

Le deuxième exemple est le rabattement classique : la demande du fichier de composition est rattrapée
par un plafond parent qu'aucun fichier du projet ne mentionne. La réponse dit à la fois combien et
pourquoi.

## Les refus

`ArgumentException` pour une source inconnue, une source répétée, une paire illisible ou une valeur
qui n'est pas un entier strictement positif — zéro n'est pas « sans limite », c'est une configuration
qui tuerait le conteneur au démarrage.

## Avant d'écrire

Prédisez la réponse quand l'option de lancement demande plus que le fichier de composition, puis
quand les deux demandent exactement la même valeur. Dites ce que la règle de départage change au
message que reçoit l'exploitant.
