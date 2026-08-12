# Solution expliquée

```sql
SELECT Status, COUNT_BIG(*) AS OrderCount FROM dbo.Orders GROUP BY Status ORDER BY Status;
```

Un statut qu'aucune commande ne porte est absent du résultat, et non présent à zéro : le regroupement ne peut produire que des groupes existants. Afficher les statuts vides demanderait une table de référence et une jointure externe, ce qui est une autre question.
