# Solution expliquée

```sql
SELECT Label FROM (SELECT City AS Label FROM dbo.Customers UNION SELECT Category FROM dbo.Products) s ORDER BY Label;
```

La réunion déduplique par défaut, contrairement à sa variante qui conserve tout : c'est un tri implicite, donc un coût, mais c'est ici le comportement demandé. Le tri final porte sur le résultat combiné et ne peut pas être écrit dans l'une des deux branches.
