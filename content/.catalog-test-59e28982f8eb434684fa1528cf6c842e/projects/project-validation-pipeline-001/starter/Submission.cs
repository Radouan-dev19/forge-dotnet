using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public static class Submission
{
    // ---------------------------------------------------------------------------------------
    // À VOUS : les trois méthodes ci-dessous, et le corps de SkuAttribute.IsValid. Les modèles
    // et RunPipeline sont fournis plus bas — les modifier ferait échouer vos propres cas.
    // ---------------------------------------------------------------------------------------

    /// <summary>Rend « valide » ou les manquements « Membre:code » triés et joints par « | ».</summary>
    public static string Violations(string name, int quantity, decimal unitPrice)
    {
        throw new NotImplementedException();
    }

    /// <summary>Valide un ProductDraft dont le SKU passe par votre attribut personnalisé.</summary>
    public static string SkuVerdict(string sku)
    {
        throw new NotImplementedException();
    }

    /// <summary>Rend « 200 » ou « 422 Membre=code1,code2;Membre2=... », membres et codes triés.</summary>
    public static string ProblemReport(string name, int quantity, decimal unitPrice)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Attribut de format SKU : deux lettres ASCII majuscules, un tiret, quatre chiffres.
    /// Une valeur absente est acceptée — la présence est le travail d'un autre attribut.
    /// </summary>
    public sealed class SkuAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            throw new NotImplementedException();
        }
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
