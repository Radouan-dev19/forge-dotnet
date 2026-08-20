# Déterminer une expiration avec délai de grâce

Implémente `public static bool IsExpired(DateOnly dueDate, DateOnly today, int graceDays)`.

`dueDate` et les `graceDays` jours suivants sont valides. L’élément est expiré seulement lorsque `today` est strictement après `dueDate.AddDays(graceDays)`. Une grâce négative doit provoquer `ArgumentOutOfRangeException`.

Les tests gardent les dates assez éloignées des limites de `DateOnly` pour que `AddDays` soit valide. Écris une table avant/à/après la dernière date valide.
