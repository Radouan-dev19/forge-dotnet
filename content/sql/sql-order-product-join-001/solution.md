# Solution expliquée

```sql
SELECT p.Name, l.Quantity FROM dbo.OrderLines l JOIN dbo.Products p ON p.ProductId = l.ProductId WHERE l.OrderId = 1 ORDER BY p.ProductId;
```

La jointure part des lignes de commande, qui portent le grain voulu, et va chercher le libellé du produit : l'inverse produirait autant de lignes que de produits, y compris ceux que la commande ne contient pas. Le grain du résultat se décide avant d'écrire la jointure.
