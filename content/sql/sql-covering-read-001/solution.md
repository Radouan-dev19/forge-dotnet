# Solution expliquée

```sql
SELECT OrderId, Total FROM dbo.Orders WHERE Status = N'Paid' ORDER BY OrderId;
```

Projeter exactement les colonnes utiles est ce qui rend une lecture couvrable par un index : si le filtre et la projection tiennent dans l'index, le moteur n'a plus à revenir chercher la ligne complète. Ajouter une colonne inutile suffit à perdre cette propriété.
