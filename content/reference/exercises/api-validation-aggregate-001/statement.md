# Rapporter toutes les violations d'un seul appel

Implémentez `Submission.Validate` avec la signature fournie. La fonction reçoit un corps de demande
déjà aplati et rend la liste complète de ce qui ne va pas.

## Le format du corps

`payload` est une suite de segments séparés par des points-virgules. Chaque segment est découpé sur
son **premier** signe égal : ce qui précède est le nom du champ, ce qui suit est sa valeur. Un
segment sans signe égal porte donc une valeur vide. Les segments vides sont ignorés.

```text
payload = "quantity=5;email=agent@forge.fr;reference=AB12CD34"
```

Les noms de champ se comparent sans tenir compte de la casse. Les blancs autour d'un nom comme d'une
valeur ne comptent pas.

## Les champs attendus

Trois champs sont attendus, et cet ordre de déclaration est aussi celui du rapport :

1. `quantity` : un entier compris entre 1 et 100, bornes comprises ;
2. `email` : contient un arobase, puis un point après cet arobase ;
3. `reference` : exactement huit caractères, chacun étant une lettre majuscule ou un chiffre.

## La règle

Le rapport ne s'arrête **jamais** à la première violation. Pour chaque champ attendu, dans l'ordre de
déclaration ci-dessus :

- deux occurrences ou plus donnent `champ:duplicate`, et la valeur n'est alors pas contrôlée ;
- aucune occurrence donne `champ:required` ;
- une occurrence qui ne respecte pas sa règle donne `champ:invalid`.

Viennent ensuite les noms qui ne sont attendus par aucun champ, sous la forme `nom:unknown`, dans
l'ordre où le corps les présente — pas dans l'ordre alphabétique.

Les violations sont jointes par une virgule, sans espace. Un corps entièrement valide rend une
**chaîne vide**. Un corps absent lève `ArgumentNullException`.

## Avant d'écrire

Prédisez cinq cas : un corps valide, un corps vide, un champ répété, un corps qui cumule une valeur
hors bornes et un nom inattendu, et un corps qui présente les champs dans le désordre. Nommez ce que
coûte à un client un rapport qui s'arrête à la première erreur.
