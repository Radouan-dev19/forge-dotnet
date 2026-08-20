using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

public static class Submission
{
    // L'ordre de déclaration est celui du rapport : il ne dépend donc pas du corps reçu.
    private static readonly string[] ExpectedFields = ["quantity", "email", "reference"];

    public static string Validate(string payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        List<Field> fields = ReadFields(payload);
        var violations = new List<string>();

        foreach (string expected in ExpectedFields)
        {
            int count = 0;
            string value = string.Empty;
            foreach (Field field in fields)
            {
                if (string.Equals(field.Name, expected, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                    value = field.Value;
                }
            }

            // Un champ répété est un défaut de forme : contrôler sa valeur reviendrait à choisir
            // silencieusement laquelle des deux fait foi.
            if (count > 1)
            {
                violations.Add(expected + ":duplicate");
            }
            else if (count == 0)
            {
                violations.Add(expected + ":required");
            }
            else if (!IsValid(expected, value))
            {
                violations.Add(expected + ":invalid");
            }
        }

        foreach (Field field in fields)
        {
            if (!IsExpected(field.Name))
            {
                violations.Add(field.Name + ":unknown");
            }
        }

        return string.Join(",", violations);
    }

    private static bool IsExpected(string name)
    {
        foreach (string expected in ExpectedFields)
        {
            if (string.Equals(name, expected, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsValid(string field, string value) => field switch
    {
        "quantity" => int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int quantity)
            && quantity is >= 1 and <= 100,
        "email" => IsEmail(value),
        "reference" => IsReference(value),
        _ => false,
    };

    private static bool IsEmail(string value)
    {
        int at = value.IndexOf('@', StringComparison.Ordinal);
        return at > 0 && value.IndexOf('.', at) > at + 1;
    }

    private static bool IsReference(string value)
    {
        if (value.Length != 8)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool allowed = character is >= 'A' and <= 'Z' || character is >= '0' and <= '9';
            if (!allowed)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Découpe le corps en couples nom et valeur. Le découpage se fait sur le premier signe égal
    /// seulement : une valeur peut légitimement en contenir un autre.
    /// </summary>
    private static List<Field> ReadFields(string payload)
    {
        var fields = new List<Field>();
        foreach (string rawSegment in payload.Split(';'))
        {
            string segment = rawSegment.Trim();
            if (segment.Length == 0)
            {
                continue;
            }

            int separator = segment.IndexOf('=', StringComparison.Ordinal);
            string name = separator < 0 ? segment : segment[..separator];
            string value = separator < 0 ? string.Empty : segment[(separator + 1)..];
            fields.Add(new Field(name.Trim(), value.Trim()));
        }

        return fields;
    }

    private readonly record struct Field(string Name, string Value);
}
