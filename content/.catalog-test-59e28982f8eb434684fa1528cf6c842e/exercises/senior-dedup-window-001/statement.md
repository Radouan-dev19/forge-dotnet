# Compter les doublons qu'une fenêtre de déduplication laisse passer

Implémentez `Submission.MissedDuplicates` avec la signature fournie. Un magasin de déduplication ne
retient pas les identifiants pour toujours : il les oublie au bout d'une **fenêtre**, faute de quoi
il grossirait sans fin. Cette fenêtre est un pari — les doublons arrivent vite — et votre fonction
mesure ce que le pari coûte sur un journal réel : le nombre de doublons revenus **après** l'oubli,
donc réappliqués à tort.

## Le format du journal

Des livraisons `minute:identifiant` séparées par des points-virgules, aux minutes croissantes au
sens large. La fenêtre est donnée en minutes.

## Le comptage

Le magasin retient chaque identifiant pendant la fenêtre **depuis sa dernière livraison** — la
fenêtre glisse, toute livraison la rafraîchit, attrapée ou non. Pour chaque livraison d'un
identifiant déjà vu :

- l'écart avec la dernière livraison est au plus la fenêtre → le doublon est attrapé, rien à
  compter ;
- l'écart dépasse strictement la fenêtre → l'identifiant a été oublié, le doublon est **réappliqué
  à tort** : il compte.

```text
MissedDuplicates("3:a;5:b;9:a", 4)      →  1      (écart 6 > 4 : a est réappliqué)
MissedDuplicates("3:a;5:b;6:a", 4)      →  0      (écart 3 ≤ 4 : attrapé)
MissedDuplicates("1:a;10:a;12:a", 5)    →  1      (10 échappe, mais rafraîchit ; 12 est attrapé)
```

Le troisième exemple porte la subtilité : la livraison qui échappe à la fenêtre **rafraîchit quand
même** la mémoire — le magasin réel enregistre ce qu'il vient de traiter, il ne sait pas qu'il
s'agissait d'un doublon.

## Les refus

`ArgumentOutOfRangeException` pour une fenêtre non positive. `ArgumentException` pour un journal
vide, une entrée sans ses deux champs, une minute non numérique ou négative, ou une chronologie qui
recule.

## Avant d'écrire

Prédisez le compte pour quatre livraisons du même identifiant espacées de deux minutes sous une
fenêtre d'une minute. Puis dites ce que ce comptage change à la vraie décision d'exploitation :
agrandir la fenêtre, ou rendre le traitement idempotent pour que le doublon réappliqué ne coûte
rien ?
