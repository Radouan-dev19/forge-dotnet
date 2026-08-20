# Classer les points chauds d'une base héritée par churn et complexité

Implémentez `Submission.TopHotspots` avec la signature fournie. Devant une base inconnue, la question
n'est pas « tout comprendre » mais « où lire d'abord ». La réponse mesurable croise deux signaux que
l'historique du dépôt fournit gratuitement : le **churn** — combien de fois un fichier change — et la
**complexité** — combien il coûte à comprendre. Leur produit désigne les points chauds : le code
difficile qu'on touche tout le temps.

## Le format du relevé

Des fichiers `nom:churn:complexité` séparés par des points-virgules — le churn en nombre de
modifications sur la période, la complexité en score entier de l'analyseur.

## Ce qu'il faut produire

Les **trois** fichiers au produit churn × complexité le plus élevé — ou moins si le relevé en compte
moins —, du plus chaud au moins chaud, départagés par l'ordre ordinal des noms, joints par des
points-virgules.

```text
TopHotspots("billing.cs:40:12;utils.cs:90:2;core.cs:15:30")
  →  "billing.cs;core.cs;utils.cs"
```

L'exemple contient les deux fausses pistes classiques : l'utilitaire au churn énorme mais trivial —
le lire n'apprend rien — et le cœur complexe mais peu remanié aurait dominé un tri par complexité
seule. Le produit les remet à leur place : le fichier dangereux est celui où la difficulté rencontre
la fréquence.

Le produit se calcule en **entier large** : un fichier à cent mille modifications et cent mille de
complexité — les monstres existent — déborde le trente-deux bits.

## Les refus

`ArgumentException` pour un relevé vide, une entrée sans ses trois champs, une mesure illisible ou
négative, ou un fichier répété.

## Avant d'écrire

Prédisez le podium quand deux fichiers partagent exactement le même score, puis dites pourquoi le
rapport se limite à trois : que ferait une équipe de la liste complète des deux cents fichiers
classés — et que fait-elle d'un podium ?
