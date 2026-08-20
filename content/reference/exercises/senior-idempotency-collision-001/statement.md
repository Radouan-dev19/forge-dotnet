# Détecter les collisions de clés d'idempotence dans un journal de requêtes

Implémentez `Submission.KeyCollisions` avec la signature fournie. Une clé d'idempotence promet
qu'une même opération, relancée, ne s'applique qu'une fois. La promesse repose sur une hypothèse que
personne ne vérifie : une clé désigne **une seule** opération. Quand un client réutilise une clé pour
une opération différente — la collision — le serveur rejoue la réponse de la première et avale la
seconde en silence. Votre fonction audite un journal et relève ces collisions.

## Le format du journal

Des requêtes `clé:empreinte` séparées par des points-virgules, dans l'ordre d'arrivée. L'empreinte
résume la charge utile : deux requêtes qui portent la même opération portent la même empreinte.

## Ce qu'il faut produire

Les clés en **collision** — revues avec une empreinte différente de leur empreinte de référence, la
première vue — triées par ordre ordinal, jointes par des virgules ; la chaîne vide si le journal est
sain. Une clé revue avec la **même** empreinte est une relance légitime : c'est exactement l'usage
pour lequel la clé existe, et la classer collision condamnerait le mécanisme entier.

```text
KeyCollisions("ord-1:a1;ord-2:b2;ord-1:a1")                        →  ""
KeyCollisions("ord-1:a1;ord-1:c9")                                 →  "ord-1"
KeyCollisions("pay-7:h1;pay-8:h2;pay-7:h3;pay-8:h2;pay-9:h4;pay-9:h5")  →  "pay-7,pay-9"
```

La comparaison se fait toujours contre l'empreinte de **référence** — la première vue — et non
contre la précédente : une clé qui enchaîne deux relances légitimes puis une charge différente est en
collision, même si le journal a l'air calme entre-temps.

## Les refus

`ArgumentException` pour un journal vide ou blanc, une requête sans ses deux champs, ou un champ
vide — une clé ou une empreinte absente ne s'audite pas, elle se répare en amont.

## Avant d'écrire

Prédisez le rapport d'un journal où la même clé porte trois empreintes différentes, puis d'un journal
où deux clés différentes portent la même empreinte. Dites pourquoi le second cas n'est pas une
collision — et ce qu'il pourrait pourtant signaler sur le générateur de clés du client.
