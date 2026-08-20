using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class Submission
{
    public static int ActivePoints(string purchases, string reference)
    {
        return Parse(purchases)
            .Where(purchase => IsActive(purchase.Month, MonthIndex(reference)))
            .Sum(purchase => (int)decimal.Floor(purchase.Amount));
    }

    public static int MonthlyBonus(string purchases, string reference)
    {
        int referenceMonth = MonthIndex(reference);
        return Parse(purchases)
            .Where(purchase => IsActive(purchase.Month, referenceMonth))
            .GroupBy(purchase => purchase.Month)
            // Le seuil se mesure sur les montants du mois avant tout arrondi : deux achats de
            // 49,99 et 50,01 atteignent cent même si leurs points de base ne font que 99.
            .Count(month => month.Sum(purchase => purchase.Amount) >= 100.00m) * 20;
    }

    public static string Statement(string purchases, string reference)
    {
        int referenceMonth = MonthIndex(reference);
        List<(int Month, decimal Amount)> all = Parse(purchases);

        int active = all
            .Where(purchase => IsActive(purchase.Month, referenceMonth))
            .Sum(purchase => (int)decimal.Floor(purchase.Amount))
            + MonthlyBonus(purchases, reference);
        int expired = all
            .Where(purchase => !IsActive(purchase.Month, referenceMonth))
            .Sum(purchase => (int)decimal.Floor(purchase.Amount));

        string level = active < 100 ? "bronze" : active < 300 ? "argent" : "or";
        return $"actifs={active};expires={expired};niveau={level}";
    }

    /// <summary>Un achat est actif dans les douze mois civils se terminant au mois de référence.</summary>
    private static bool IsActive(int purchaseMonth, int referenceMonth)
    {
        int age = referenceMonth - purchaseMonth;
        return age >= 0 && age <= 11;
    }

    /// <summary>Index de mois civil : année fois douze plus mois, ce qui rend l'écart soustractif.</summary>
    private static int MonthIndex(string date)
    {
        DateOnly parsed = DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        return (parsed.Year * 12) + parsed.Month - 1;
    }

    private static List<(int Month, decimal Amount)> Parse(string purchases)
    {
        if (purchases.Length == 0)
        {
            return [];
        }

        var parsed = new List<(int Month, decimal Amount)>();
        foreach (string entry in purchases.Split(';'))
        {
            // La date fait dix caractères : couper à la première colonne casserait le montant.
            string date = entry[..10];
            decimal amount = decimal.Parse(entry[11..], CultureInfo.InvariantCulture);
            parsed.Add((MonthIndex(date), amount));
        }

        return parsed;
    }
}
