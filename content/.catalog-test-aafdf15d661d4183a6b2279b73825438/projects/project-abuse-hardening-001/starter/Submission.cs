using System;

public static class Submission
{
    // ---------------------------------------------------------------------------------------
    // À VOUS : les trois défenses. Le brief est le contrat complet — les règles y sont écrites
    // dans l'ordre exact où elles s'appliquent, et les cas cachés contiennent des variantes
    // hostiles que l'énoncé ne liste pas.
    // ---------------------------------------------------------------------------------------

    /// <summary>Rend « authentique » ou « refus » après recalcul HMAC-SHA256 et comparaison en temps constant.</summary>
    public static string SignatureVerdict(string payload, string secret, string signature)
    {
        throw new NotImplementedException();
    }

    /// <summary>Canonise le chemin demandé puis rend le chemin sûr ou « refus:motif ».</summary>
    public static string SafePath(string requested)
    {
        throw new NotImplementedException();
    }

    /// <summary>Rend « admis », « refus:horodatage » ou « refus:rejeu » selon le journal et la fenêtre.</summary>
    public static string AdmitVerdict(string journal, string request, int windowSeconds)
    {
        throw new NotImplementedException();
    }
}
