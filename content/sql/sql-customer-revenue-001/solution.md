# Solution expliquée

```sql
SELECT CustomerId, SUM(Total) AS Revenue FROM dbo.Orders GROUP BY CustomerId ORDER BY CustomerId;
```

Le regroupement se fait sur la table des commandes seule : passer par les clients ajouterait une jointure sans changer le résultat, puisque tout client absent de la table des commandes n'a rien à sommer. Choisir la plus petite table de départ qui répond à la question est un réflexe de coût.
