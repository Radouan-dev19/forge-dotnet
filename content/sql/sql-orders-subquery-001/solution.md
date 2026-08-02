# Solution expliquée

```sql
SELECT p.ProductId, p.ProductName
FROM dbo.Products AS p
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.OrderLines AS line
    WHERE line.ProductId = p.ProductId
)
ORDER BY p.ProductId;
```

La sous‑requête est évaluée logiquement pour le produit courant. `SELECT 1` indique que seule l’existence compte. L’anti‑jointure reste correcte même si d’autres colonnes de la ligne deviennent nullables.

Le test vérifie la valeur exacte, puis détruit une ligne avant d’exécuter `reset.sql` et de recompter les trois lignes de commande.
