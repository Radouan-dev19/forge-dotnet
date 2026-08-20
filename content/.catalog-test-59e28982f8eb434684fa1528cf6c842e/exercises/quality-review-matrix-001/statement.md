# Graduer la sévérité d'un constat de revue par sa matrice

Implémentez `Submission.ReviewSeverity` avec la signature fournie. Votre équipe a remplacé le
« ressenti du relecteur » par un barème écrit : chaque constat de revue se décrit par trois attributs,
et la sévérité se calcule au lieu de se négocier.

## Le format d'un constat

Trois paires clé-valeur séparées par des points-virgules, dans n'importe quel ordre :

- `category` — la nature du constat : `security`, `correctness`, `performance` ou `style` ;
- `reachable` — l'atteignabilité du code visé : `always`, `feature-flag` ou `dead-code` ;
- `blast` — le rayon touché en cas de problème : `system`, `module` ou `line`.

## Le barème

Chaque valeur rapporte des points : `security` 4, `correctness` 3, `performance` 2, `style` 1 ;
`always` 2, `feature-flag` 1, `dead-code` 0 ; `system` 2, `module` 1, `line` 0. La somme se lit dans
l'échelle :

| Somme | Sévérité |
|---|---|
| 7 et plus | `blocker` |
| 5 à 6 | `major` |
| 3 à 4 | `minor` |
| 2 et moins | `nit` |

```text
ReviewSeverity("category=correctness;reachable=always;blast=module")  →  "major"
ReviewSeverity("category=style;reachable=dead-code;blast=line")       →  "nit"
```

## Ce que le barème impose au calcul

Rien ne doit contourner la table : pas de règle spéciale « la sécurité bloque toujours », pas de
plancher caché. Un constat de sécurité sur une seule ligne d'un chemin actif vaut ses six points — un
`major` sérieux, pas un `blocker` automatique. C'est la condition pour que l'équipe fasse confiance au
chiffre : si le code ajoute des exceptions que le barème n'écrit pas, le barème redevient un ressenti.

## Les refus

`ArgumentException` pour un constat vide, une paire illisible, une clé hors des trois attendues, une
clé répétée, une valeur hors vocabulaire, ou un attribut manquant. Compléter par défaut fabriquerait
une sévérité que personne n'a déclarée.

## Avant d'écrire

Calculez à la main la somme d'un constat de performance derrière un drapeau de fonctionnalité à
l'échelle du système, et celle d'un constat de justesse dans du code mort à rayon système. Dites si
l'un des deux vous surprend, et ce que cela dirait du barème.
