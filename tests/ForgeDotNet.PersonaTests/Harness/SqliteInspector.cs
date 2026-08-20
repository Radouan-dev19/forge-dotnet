using Microsoft.Data.Sqlite;

namespace ForgeDotNet.PersonaTests.Harness;

/// <summary>
/// Lecture seule de la base SQLite d'un persona, pour les assertions sur l'état persistant.
/// Jamais d'écriture ici : le seul cas d'écriture assumé est la translation temporelle de P7,
/// portée par une méthode explicitement nommée et documentée.
/// </summary>
public static class SqliteInspector
{
    public static long Scalar(string databasePath, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        object? result = command.ExecuteScalar();
        return result is null or DBNull ? 0L : Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static string? Text(string databasePath, string sql)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar() as string;
    }

    /// <summary>
    /// Translation temporelle de P7 : recule uniformément les horodatages persistés, application
    /// arrêtée, pour simuler quatorze jours d'absence. Le produit n'expose volontairement aucune
    /// horloge réglable — elle serait un canal de falsification de récence — et l'horloge système
    /// n'est jamais touchée. Voir le registre de P7 pour la justification complète.
    /// </summary>
    public static int ShiftPersistedTimestamps(string databasePath, TimeSpan shift)
    {
        using var connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        int updated = 0;
        var targets = new List<(string Table, string Column, bool IsDate)>();
        using (SqliteCommand tables = connection.CreateCommand())
        {
            tables.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name <> '__EFMigrationsHistory'";
            using SqliteDataReader reader = tables.ExecuteReader();
            var names = new List<string>();
            while (reader.Read())
            {
                names.Add(reader.GetString(0));
            }

            reader.Close();
            foreach (string table in names)
            {
                using SqliteCommand columns = connection.CreateCommand();
                columns.CommandText = $"PRAGMA table_info(\"{table}\")";
                using SqliteDataReader columnReader = columns.ExecuteReader();
                while (columnReader.Read())
                {
                    string column = columnReader.GetString(1);
                    if (column.EndsWith("Utc", StringComparison.Ordinal)
                        || column.EndsWith("AtUtc", StringComparison.Ordinal))
                    {
                        targets.Add((table, column, IsDate: false));
                    }
                    else if (column.EndsWith("On", StringComparison.Ordinal)
                        && (column.Contains("Due", StringComparison.Ordinal)
                            || column.Contains("Reviewed", StringComparison.Ordinal)))
                    {
                        targets.Add((table, column, IsDate: true));
                    }
                }
            }
        }

        foreach ((string table, string column, bool isDate) in targets)
        {
            using SqliteCommand update = connection.CreateCommand();
            string offset = $"-{(int)shift.TotalDays} days";
            update.CommandText = isDate
                ? $"UPDATE \"{table}\" SET \"{column}\" = date(\"{column}\", '{offset}') WHERE \"{column}\" IS NOT NULL"
                : $"UPDATE \"{table}\" SET \"{column}\" = strftime('%Y-%m-%d %H:%M:%f', \"{column}\", '{offset}') || substr(\"{column}\", instr(\"{column}\", '+')) WHERE \"{column}\" IS NOT NULL AND instr(\"{column}\", '+') > 0";
            try
            {
                updated += update.ExecuteNonQuery();
            }
            catch (SqliteException)
            {
                // Une colonne au format inattendu est laissée telle quelle : la translation reste
                // partielle plutôt que corrompue, et les assertions du persona le verront.
            }

            if (!isDate)
            {
                using SqliteCommand plain = connection.CreateCommand();
                plain.CommandText =
                    $"UPDATE \"{table}\" SET \"{column}\" = strftime('%Y-%m-%d %H:%M:%f', \"{column}\", '-{(int)shift.TotalDays} days') WHERE \"{column}\" IS NOT NULL AND instr(\"{column}\", '+') = 0 AND \"{column}\" <> ''";
                try
                {
                    updated += plain.ExecuteNonQuery();
                }
                catch (SqliteException)
                {
                }
            }
        }

        return updated;
    }
}
