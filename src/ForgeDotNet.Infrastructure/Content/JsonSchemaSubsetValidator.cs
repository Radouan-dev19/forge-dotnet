using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForgeDotNet.Domain.Content;

namespace ForgeDotNet.Infrastructure.Content;

internal static partial class JsonSchemaSubsetValidator
{
    public static void Validate(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        ValidateType(instance, schema, filePath, propertyPath, issues);
        ValidateConstAndEnum(instance, schema, filePath, propertyPath, issues);

        if (instance.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(instance, schema, filePath, propertyPath, issues);
        }
        else if (instance.ValueKind == JsonValueKind.Array)
        {
            ValidateArray(instance, schema, filePath, propertyPath, issues);
        }
        else if (instance.ValueKind == JsonValueKind.String)
        {
            ValidateString(instance, schema, filePath, propertyPath, issues);
        }
        else if (instance.ValueKind == JsonValueKind.Number)
        {
            ValidateNumber(instance, schema, filePath, propertyPath, issues);
        }
    }

    private static void ValidateType(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (!schema.TryGetProperty("type", out JsonElement typeElement))
        {
            return;
        }

        string expectedType = typeElement.GetString() ?? string.Empty;
        bool matches = expectedType switch
        {
            "object" => instance.ValueKind == JsonValueKind.Object,
            "array" => instance.ValueKind == JsonValueKind.Array,
            "string" => instance.ValueKind == JsonValueKind.String,
            "integer" => instance.ValueKind == JsonValueKind.Number && IsInteger(instance),
            "number" => instance.ValueKind == JsonValueKind.Number,
            "boolean" => instance.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "null" => instance.ValueKind == JsonValueKind.Null,
            _ => false,
        };

        if (!matches)
        {
            AddIssue(issues, "type", filePath, propertyPath, $"Type attendu : {expectedType}.");
        }
    }

    private static void ValidateConstAndEnum(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (schema.TryGetProperty("const", out JsonElement constant)
            && !JsonElement.DeepEquals(instance, constant))
        {
            AddIssue(issues, "const", filePath, propertyPath, "Valeur non autorisée pour cette version du schéma.");
        }

        if (schema.TryGetProperty("enum", out JsonElement enumeration)
            && !enumeration.EnumerateArray().Any(candidate => JsonElement.DeepEquals(instance, candidate)))
        {
            string allowed = string.Join(", ", enumeration.EnumerateArray().Select(FormatValue));
            AddIssue(issues, "enum", filePath, propertyPath, $"Valeur attendue parmi : {allowed}.");
        }
    }

    private static void ValidateObject(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (schema.TryGetProperty("required", out JsonElement required))
        {
            foreach (JsonElement propertyName in required.EnumerateArray())
            {
                string name = propertyName.GetString()!;
                if (!instance.TryGetProperty(name, out _))
                {
                    AddIssue(
                        issues,
                        "required",
                        filePath,
                        AppendProperty(propertyPath, name),
                        $"Propriété obligatoire absente : {name}.");
                }
            }
        }

        JsonElement properties = default;
        bool hasProperties = schema.TryGetProperty("properties", out properties);
        foreach (JsonProperty property in instance.EnumerateObject())
        {
            string childPath = AppendProperty(propertyPath, property.Name);
            if (hasProperties && properties.TryGetProperty(property.Name, out JsonElement propertySchema))
            {
                Validate(property.Value, propertySchema, filePath, childPath, issues);
            }
            else if (schema.TryGetProperty("additionalProperties", out JsonElement additional)
                && additional.ValueKind == JsonValueKind.False)
            {
                AddIssue(issues, "additionalProperties", filePath, childPath, "Propriété inconnue interdite.");
            }
        }
    }

    private static void ValidateArray(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        int length = instance.GetArrayLength();
        if (schema.TryGetProperty("minItems", out JsonElement minimumItems)
            && length < minimumItems.GetInt32())
        {
            AddIssue(issues, "minItems", filePath, propertyPath, $"Au moins {minimumItems.GetInt32()} élément(s) requis.");
        }

        if (schema.TryGetProperty("maxItems", out JsonElement maximumItems)
            && length > maximumItems.GetInt32())
        {
            AddIssue(issues, "maxItems", filePath, propertyPath, $"Au plus {maximumItems.GetInt32()} élément(s) autorisé(s).");
        }

        if (schema.TryGetProperty("uniqueItems", out JsonElement uniqueItems)
            && uniqueItems.ValueKind == JsonValueKind.True)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int index = 0;
            foreach (JsonElement item in instance.EnumerateArray())
            {
                if (!seen.Add(item.GetRawText()))
                {
                    AddIssue(issues, "uniqueItems", filePath, $"{propertyPath}[{index}]", "Élément dupliqué interdit.");
                }

                index++;
            }
        }

        JsonElement.ArrayEnumerator items = instance.EnumerateArray();
        JsonElement[] values = items.ToArray();
        int prefixCount = 0;
        if (schema.TryGetProperty("prefixItems", out JsonElement prefixItems))
        {
            JsonElement[] prefixSchemas = prefixItems.EnumerateArray().ToArray();
            prefixCount = prefixSchemas.Length;
            for (int index = 0; index < Math.Min(values.Length, prefixSchemas.Length); index++)
            {
                Validate(values[index], prefixSchemas[index], filePath, $"{propertyPath}[{index}]", issues);
            }
        }

        if (schema.TryGetProperty("items", out JsonElement itemSchema))
        {
            if (itemSchema.ValueKind == JsonValueKind.False && values.Length > prefixCount)
            {
                AddIssue(issues, "items", filePath, propertyPath, "Éléments supplémentaires interdits.");
            }
            else if (itemSchema.ValueKind == JsonValueKind.Object)
            {
                for (int index = prefixCount; index < values.Length; index++)
                {
                    Validate(values[index], itemSchema, filePath, $"{propertyPath}[{index}]", issues);
                }
            }
        }
    }

    private static void ValidateString(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        string value = instance.GetString()!;
        if (schema.TryGetProperty("minLength", out JsonElement minimumLength)
            && value.Length < minimumLength.GetInt32())
        {
            AddIssue(issues, "minLength", filePath, propertyPath, $"Longueur minimale : {minimumLength.GetInt32()} caractère(s).");
        }

        if (schema.TryGetProperty("maxLength", out JsonElement maximumLength)
            && value.Length > maximumLength.GetInt32())
        {
            AddIssue(issues, "maxLength", filePath, propertyPath, $"Longueur maximale : {maximumLength.GetInt32()} caractère(s).");
        }

        if (schema.TryGetProperty("pattern", out JsonElement pattern)
            && !Regex.IsMatch(value, pattern.GetString()!, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1)))
        {
            AddIssue(issues, "pattern", filePath, propertyPath, "Format de texte invalide.");
        }
    }

    private static void ValidateNumber(
        JsonElement instance,
        JsonElement schema,
        string filePath,
        string propertyPath,
        ICollection<ContentValidationIssue> issues)
    {
        if (!instance.TryGetDecimal(out decimal value))
        {
            AddIssue(issues, "number", filePath, propertyPath, "Nombre hors plage prise en charge.");
            return;
        }

        if (schema.TryGetProperty("minimum", out JsonElement minimum)
            && value < minimum.GetDecimal())
        {
            AddIssue(issues, "minimum", filePath, propertyPath, $"Valeur minimale : {minimum.GetDecimal().ToString(CultureInfo.InvariantCulture)}.");
        }

        if (schema.TryGetProperty("exclusiveMinimum", out JsonElement exclusiveMinimum)
            && value <= exclusiveMinimum.GetDecimal())
        {
            AddIssue(issues, "exclusiveMinimum", filePath, propertyPath, $"La valeur doit être supérieure à {exclusiveMinimum.GetDecimal().ToString(CultureInfo.InvariantCulture)}.");
        }

        if (schema.TryGetProperty("maximum", out JsonElement maximum)
            && value > maximum.GetDecimal())
        {
            AddIssue(issues, "maximum", filePath, propertyPath, $"Valeur maximale : {maximum.GetDecimal().ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    private static bool IsInteger(JsonElement element) =>
        element.TryGetInt64(out _)
        || (element.TryGetDecimal(out decimal number) && decimal.Truncate(number) == number);

    private static string AppendProperty(string parent, string property) => $"{parent}.{property}";

    private static string FormatValue(JsonElement element) => element.ValueKind == JsonValueKind.String
        ? $"'{element.GetString()}'"
        : element.GetRawText();

    private static void AddIssue(
        ICollection<ContentValidationIssue> issues,
        string code,
        string filePath,
        string propertyPath,
        string message) => issues.Add(new ContentValidationIssue(code, filePath, propertyPath, message));
}
