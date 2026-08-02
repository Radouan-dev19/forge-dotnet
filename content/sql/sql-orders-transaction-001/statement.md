# Objectif observable

Réserver deux unités sans lecture‑puis‑écriture vulnérable à la concurrence.

Écris une seule instruction qui cible le produit `1`, vérifie que la quantité suffit, retire `2` et retourne `ProductId` et la nouvelle `Quantity`. Le résultat dans la transaction est `1, 3`.

Utilise un prédicat atomique `Quantity >= 2` et `OUTPUT inserted...`. Le laboratoire exécute l’instruction dans une transaction protégée puis effectue un rollback : la quantité persistée doit rester `5`.

Explique le rôle d’un verrou de mise à jour et la différence entre cohérence de cette écriture atomique et promesse générale du niveau d’isolation. Le test négatif tente une soustraction non gardée et doit être rejeté ou contredit par la contrainte.
