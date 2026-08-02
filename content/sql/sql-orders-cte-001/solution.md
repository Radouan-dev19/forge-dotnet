# Solution expliquée

```sql
WITH MonthlyRevenue AS (
    SELECT CONVERT(char(7), CreatedAtUtc, 126) AS RevenueMonth,
           SUM(Total) AS PaidRevenue
    FROM dbo.Orders
    WHERE Status = N'Paid'
    GROUP BY CONVERT(char(7), CreatedAtUtc, 126)
)
SELECT RevenueMonth, PaidRevenue
FROM MonthlyRevenue
WHERE PaidRevenue > 100
ORDER BY RevenueMonth;
```

La CTE nomme une étape logique sans matérialisation promise. Elle rend explicites le filtre de statut et le grain mensuel. La requête externe peut alors filtrer l’alias agrégé sans répéter `SUM`.

Le test compare le mois et la somme, sans supposer un plan ou un coût exact.
