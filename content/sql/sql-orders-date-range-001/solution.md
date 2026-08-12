# Solution expliquée

```sql
SELECT OrderId, OrderDate FROM dbo.Orders WHERE OrderDate >= '2026-02-01' AND OrderDate < '2026-03-01' ORDER BY OrderId;
```

La plage s'exprime bornée à gauche et ouverte à droite plutôt que par deux inégalités larges : une date portant une heure au dernier jour du mois serait sinon exclue. Cette forme reste également juste si la colonne gagne un jour une composante horaire.
