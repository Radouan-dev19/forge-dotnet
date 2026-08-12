# Solution expliquée

```sql
SELECT OrderId, Total FROM dbo.Orders ORDER BY OrderId;
```

Nommer les colonnes plutôt que les demander toutes n'est pas une préférence de style : la projection explicite fixe le contrat de la requête, si bien qu'une colonne ajoutée plus tard au schéma ne changera ni le résultat ni le volume transféré.
