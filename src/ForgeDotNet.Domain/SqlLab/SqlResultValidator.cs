using System.Globalization;

namespace ForgeDotNet.Domain.SqlLab;

public static class SqlResultValidator
{
    public static SqlLabValidationResult Validate(
        SqlLabExpectedResult expectation,
        SqlLabResultSet? actual)
    {
        ArgumentNullException.ThrowIfNull(expectation);
        var issues = new List<string>();
        if (actual is null)
        {
            return new SqlLabValidationResult(false, ["La requête ne retourne aucun jeu de résultats."]);
        }

        string[] actualColumns = actual.Columns.Select(column => column.Name).ToArray();
        if (!expectation.Columns.SequenceEqual(actualColumns, StringComparer.OrdinalIgnoreCase))
        {
            issues.Add(
                $"Colonnes attendues : {string.Join(", ", expectation.Columns)} ; reçues : {string.Join(", ", actualColumns)}.");
        }

        if (expectation.Rows.Count != actual.Rows.Count)
        {
            issues.Add($"Nombre de lignes attendu : {expectation.Rows.Count} ; reçu : {actual.Rows.Count}.");
        }

        if (issues.Count == 0)
        {
            IReadOnlyList<IReadOnlyList<SqlLabCell>> expectedRows = expectation.Ordered
                ? expectation.Rows
                : expectation.Rows.OrderBy(CanonicalRow, StringComparer.Ordinal).ToArray();
            IReadOnlyList<IReadOnlyList<SqlLabCell>> actualRows = expectation.Ordered
                ? actual.Rows
                : actual.Rows.OrderBy(CanonicalRow, StringComparer.Ordinal).ToArray();

            for (int rowIndex = 0; rowIndex < expectedRows.Count; rowIndex++)
            {
                if (!RowsEqual(expectedRows[rowIndex], actualRows[rowIndex], expectation.NumericTolerance))
                {
                    issues.Add($"La ligne {rowIndex + 1} ne correspond pas au résultat attendu.");
                }
            }
        }

        return new SqlLabValidationResult(issues.Count == 0, issues);
    }

    private static bool RowsEqual(
        IReadOnlyList<SqlLabCell> expected,
        IReadOnlyList<SqlLabCell> actual,
        decimal tolerance)
    {
        if (expected.Count != actual.Count)
        {
            return false;
        }

        for (int index = 0; index < expected.Count; index++)
        {
            SqlLabCell expectedCell = expected[index];
            SqlLabCell actualCell = actual[index];
            if (expectedCell.IsNull || actualCell.IsNull)
            {
                if (expectedCell.IsNull != actualCell.IsNull)
                {
                    return false;
                }

                continue;
            }

            if (decimal.TryParse(expectedCell.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal expectedNumber)
                && decimal.TryParse(actualCell.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal actualNumber))
            {
                if (Math.Abs(expectedNumber - actualNumber) > tolerance)
                {
                    return false;
                }
            }
            else if (!string.Equals(expectedCell.Value, actualCell.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string CanonicalRow(IReadOnlyList<SqlLabCell> row) => string.Join(
        '\u001f',
        row.Select(cell => cell.IsNull ? "<NULL>" : cell.Value?.Normalize() ?? string.Empty));
}
