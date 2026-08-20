# Chiffrer la disponibilité composée d'une chaîne d'appels synchrones

Implémentez `Submission.ChainAvailability` avec la signature fournie. Quand une requête traverse
plusieurs services en synchrone, il suffit qu'**un seul** échoue pour qu'elle échoue : les
disponibilités ne se moyennent pas, elles se **multiplient**. Votre fonction chiffre ce que promet
réellement une chaîne — le nombre que l'architecte devrait annoncer à la place du plus flatteur.

## Le calcul

La fonction reçoit les disponibilités des maillons en pourcentage, séparées par des points-virgules :
`"99.9;99.9;99.5"`. Elle rend la disponibilité composée : le produit des disponibilités, ramené en
pourcentage, **plancher au centième** — on n'annonce jamais plus qu'on ne tient, et l'arrondi au
plus proche offrirait parfois le centième manquant.

```text
ChainAvailability("99.9;99.9;99.5")  →  99.3
ChainAvailability("99.99;99.99")     →  99.98
ChainAvailability("99;99;99;99;99")  →  95.09
```

Le troisième exemple est celui qui réveille les réunions d'architecture : cinq maillons à deux
neufs — un chiffre honorable isolément — composent un ensemble sous 95,1 %, soit plus de quatre cents
heures d'indisponibilité potentielle par an. Le calcul vit en **décimal exact** : les pourcentages
comme 99,9 n'ont pas de représentation flottante binaire, et le centième final dériverait selon
l'ordre des maillons.

## Les refus

`ArgumentException` pour une chaîne vide, un maillon illisible, nul, négatif ou au-delà de cent, ou
une chaîne de **plus de dix maillons** — au-delà, le calcul n'est plus une information mais un
constat : la chaîne elle-même est le problème à traiter, par découplage asynchrone ou par fusion de
maillons.

## Avant d'écrire

Prédisez la disponibilité composée de deux maillons parfaits à cent pour cent, puis l'effet d'ajouter
un troisième maillon à 99,9. Dites pourquoi le plancher au centième — plutôt que l'arrondi au plus
proche — est le seul choix cohérent avec ce que ce chiffre promet à un client.
