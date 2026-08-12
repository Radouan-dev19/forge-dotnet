# Solution expliquée

```sql
SELECT OrderId, SUM(Total) OVER (ORDER BY OrderId ROWS UNBOUNDED PRECEDING) AS RunningTotal FROM dbo.Orders ORDER BY OrderId;
```

Le cadre de la fenêtre est déclaré explicitement plutôt que laissé au défaut : sur une somme ordonnée, le cadre implicite s'arrête au dernier pair, si bien que deux lignes de même clé de tri afficheraient le même cumul. Déclarer le cadre supprime cette dépendance à une valeur par défaut.
