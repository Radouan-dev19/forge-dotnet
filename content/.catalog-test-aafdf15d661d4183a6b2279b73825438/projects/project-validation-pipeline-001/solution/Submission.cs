using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public static class Submission
{
    public static string Violations(string name, int quantity, decimal unitPrice)
    {
        var draft = new OrderDraft { Name = name, Quantity = quantity, UnitPrice = unitPrice };
        List<string> entries = DescribeFailures(RunPipeline(draft));
        return entries.Count == 0 ? "valide" : string.Join("|", entries);
    }

    public static string SkuVerdict(string sku)
    {
        var draft = new ProductDraft { Sku = sku };
        List<string> entries = DescribeFailures(RunPipeline(draft));
        return entries.Count == 0 ? "valide" : string.Join("|", entries);
    }

    public static string ProblemReport(string name, int quantity, decimal unitPrice)
    {
        var draft = new OrderDraft { Name = name, Quantity = quantity, UnitPrice = unitPrice };
        List<string> entries = DescribeFailures(RunPipeline(draft));
        if (entries.Count == 0)
        {
            return "200";
        }

        // Les entrées « Membre:code » sont déjà triées : le groupement par membre conserve donc
        // à la fois l'ordre des membres et celui des codes d'un même membre.
        IEnumerable<string> groups = entries
            .Select(entry => entry.Split(':', 2))
            .GroupBy(parts => parts[0], StringComparer.Ordinal)
            .Select(group => $"{group.Key}={string.Join(",", group.Select(parts => parts[1]))}");
        return "422 " + string.Join(";", groups);
    }

    /// <summary>Aplatit les résultats du pipeline en entrées « Membre:code » triées.</summary>
    private static List<string> DescribeFailures(List<ValidationResult> results)
    {
        var entries = new List<string>();
        foreach (ValidationResult result in results)
        {
            foreach (string member in result.MemberNames)
            {
                entries.Add($"{member}:{result.ErrorMessage}");
            }
        }

        // L'ordre de collecte du validateur n'est pas contractuel : seul ce tri l'est.
        entries.Sort(StringComparer.Ordinal);
        return entries;
    }

    /// <summary>
    /// Attribut de format SKU : deux lettres ASCII majuscules, un tiret, quatre chiffres.
    /// Une valeur absente est acceptée — la présence est le travail d'un autre attribut.
    /// </summary>
    public sealed class SkuAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is null)
            {
                // La convention du pipeline : un attribut de format laisse passer l'absence,
                // sinon il devient impossible de composer présence et format séparément.
                return true;
            }

            if (value is not string sku || sku.Length != 7)
            {
                return false;
            }

            bool lettersValid = IsAsciiUpper(sku[0]) && IsAsciiUpper(sku[1]);
            bool digitsValid = IsAsciiDigit(sku[3]) && IsAsciiDigit(sku[4]) && IsAsciiDigit(sku[5]) && IsAsciiDigit(sku[6]);
            return lettersValid && sku[2] == '-' && digitsValid;
        }

        // char.IsUpper accepterait « É » : majuscule ne veut pas dire ASCII, d'où les bornes explicites.
        private static bool IsAsciiUpper(char value) => value is >= 'A' and <= 'Z';

        private static bool IsAsciiDigit(char value) => value is >= '0' and <= '9';
    }

    // ======================= FOURNI — ne pas modifier =======================

    public sealed class OrderDraft
    {
        [Required(ErrorMessage = "obligatoire")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "longueur")]
        [RegularExpression("^[A-Za-zÀ-ÿ0-9 '-]+$", ErrorMessage = "caracteres")]
        public string? Name { get; set; }

        [Range(1, 100, ErrorMessage = "intervalle")]
        public int Quantity { get; set; }

        [Range(0.01, 999.99, ErrorMessage = "intervalle")]
        public decimal UnitPrice { get; set; }
    }

    public sealed class ProductDraft
    {
        [Sku(ErrorMessage = "sku")]
        public string? Sku { get; set; }
    }

    /// <summary>L'invocation de référence du pipeline : toutes les propriétés, tous les attributs.</summary>
    public static List<ValidationResult> RunPipeline(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }
}
