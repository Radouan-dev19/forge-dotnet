# Solution expliquée

```sql
SELECT OrderId, OrderDate FROM dbo.Orders WHERE OrderDate > '2026-02-10' OR (OrderDate = '2026-02-10' AND OrderId > 2) ORDER BY OrderDate, OrderId;
```

La condition de reprise doit reproduire exactement l'ordre annoncé : sur une clé en deux parties, cela s'écrit « date strictement postérieure, ou date égale et identifiant strictement supérieur ». Une reprise portant sur la seule date sauterait les commandes du même jour restées à lire.
