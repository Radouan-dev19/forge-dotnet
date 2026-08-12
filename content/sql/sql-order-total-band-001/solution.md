# Solution expliquée

```sql
SELECT OrderId, CASE WHEN Total < 30 THEN N'small' WHEN Total < 80 THEN N'medium' ELSE N'large' END AS Band FROM dbo.Orders ORDER BY OrderId;
```

L'expression conditionnelle est évaluée dans l'ordre écrit et s'arrête à la première branche vraie : les bornes hautes ne se répètent donc pas d'une branche à l'autre. Écrire des conditions qui se recouvrent fonctionnerait ici par accident, et deviendrait faux au premier réordonnancement.
