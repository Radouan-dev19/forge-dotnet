using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class Submission
{
    public static int CountValidRows(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        int valid = 0;
        foreach (Row row in ReadRows(csv))
        {
            if (row.Rejection is null)
            {
                valid++;
            }
        }

        return valid;
    }

    public static string RejectionReport(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var lines = new List<string>();
        foreach (Row row in ReadRows(csv))
        {
            if (row.Rejection is not null)
            {
                lines.Add($"ligne {row.Number} : {row.Rejection}");
            }
        }

        return string.Join("\n", lines);
    }

    public static string ImportReport(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        int valid = 0;
        int rejected = 0;
        decimal total = 0m;
        foreach (Row row in ReadRows(csv))
        {
            if (row.Rejection is not null)
            {
                rejected++;
                continue;
            }

            valid++;
            // La somme reste exacte ; l'arrondi n'intervient qu'une fois, plus bas.
            total += row.Quantity * row.Price;
        }

        decimal rounded = decimal.Round(total, 2, MidpointRounding.AwayFromZero);
        return "{\"valides\":" + valid.ToString(CultureInfo.InvariantCulture)
            + ",\"rejetees\":" + rejected.ToString(CultureInfo.InvariantCulture)
            + ",\"total\":" + rounded.ToString("F2", CultureInfo.InvariantCulture)
            + "}";
    }

    private static List<Row> ReadRows(string csv)
    {
        var rows = new List<Row>();
        string[] lines = csv.Split('\n');
        for (int index = 1; index < lines.Length; index++)
        {
            string line = lines[index].TrimEnd('\r');
            if (line.Trim().Length == 0)
            {
                continue;
            }

            rows.Add(ReadRow(line, index + 1));
        }

        return rows;
    }

    private static Row ReadRow(string line, int number)
    {
        string[] fields = line.Split(';');
        if (fields.Length != 3)
        {
            return new Row(number, 0, 0m, "champs");
        }

        if (fields[0].Trim().Length == 0)
        {
            return new Row(number, 0, 0m, "reference");
        }

        if (!int.TryParse(fields[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int quantity)
            || quantity <= 0)
        {
            return new Row(number, 0, 0m, "quantite");
        }

        if (!decimal.TryParse(fields[2].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price)
            || price < 0m)
        {
            return new Row(number, 0, 0m, "prix");
        }

        return new Row(number, quantity, price, null);
    }

    private sealed record Row(int Number, int Quantity, decimal Price, string? Rejection);
}
