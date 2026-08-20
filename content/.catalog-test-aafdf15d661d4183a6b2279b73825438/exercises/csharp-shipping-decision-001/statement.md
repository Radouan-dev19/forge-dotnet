# Décider des frais de livraison

Implémente `public static decimal ShippingCost(decimal orderTotal, bool isExpress)`.

Un envoi express coûte toujours `9.90m`. Un envoi standard est gratuit lorsque `orderTotal` vaut au moins `80m`, sinon il coûte `4.90m`. Un total négatif doit provoquer `ArgumentOutOfRangeException`.

La borne `80m` est incluse. Ne change ni `Submission` ni la signature. Avant de coder, écris l’ordre des décisions et justifie la priorité du mode express.
