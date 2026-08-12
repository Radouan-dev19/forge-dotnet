# Solution expliquée

```sql
SELECT TOP (2) OrderId, Total FROM dbo.Orders WHERE Total > 70 OR (Total = 70 AND OrderId > 3) ORDER BY Total, OrderId;
```

La pagination par clé se lit sur la dernière ligne rendue, jamais sur un décalage : une commande insérée entre deux appels ne décale donc rien. C'est ce qui la distingue d'une pagination par rang, dont chaque page dépend de tout ce qui la précède.
