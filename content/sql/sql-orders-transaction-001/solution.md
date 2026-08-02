# Solution expliquée

```sql
UPDATE dbo.Inventory WITH (UPDLOCK, ROWLOCK)
SET Quantity = Quantity - 2
OUTPUT inserted.ProductId, inserted.Quantity
WHERE ProductId = 1 AND Quantity >= 2;
```

Le prédicat et la modification appartiennent à la même instruction : aucune valeur lue par le client ne devient périmée entre deux commandes. `UPDLOCK` demande un verrou compatible avec l’intention d’écrire ; `ROWLOCK` reste une indication et non une garantie absolue.

Le test observe `3` avant rollback, puis `5` depuis une nouvelle connexion. Il vérifie aussi le reset. Cette preuve porte sur les effets, pas sur un délai ou un verrou interne fragile.
