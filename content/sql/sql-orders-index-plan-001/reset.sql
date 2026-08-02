DELETE FROM dbo.Orders;
WITH Numbers AS (SELECT 1 AS n UNION ALL SELECT n + 1 FROM Numbers WHERE n < 20000)
INSERT dbo.Orders (OrderId, CustomerId, Total, CreatedAtUtc)
SELECT n, CASE WHEN n > 19990 THEN 777 ELSE (n % 20) + 1 END, CAST((n % 5000) / 10.0 AS decimal(10,2)), DATEADD(minute, n, CAST('2026-01-01' AS datetime2))
FROM Numbers OPTION (MAXRECURSION 0);
