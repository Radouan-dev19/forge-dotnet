using System;
using System.Globalization;

public static class Submission
{
    public static decimal ChainAvailability(string availabilities)
    {
        if (string.IsNullOrWhiteSpace(availabilities))
        {
            throw new ArgumentException("Une chaîne vide ne promet rien.", nameof(availabilities));
        }

        string[] links = availabilities.Split(';');

        // Au-delà de dix maillons synchrones, le chiffre ne renseigne plus : la chaîne elle-même
        // est le problème, à traiter par découplage ou par fusion.
        if (links.Length > 10)
        {
            throw new ArgumentException(
                "Une chaîne de plus de dix maillons est déjà la réponse.", nameof(availabilities));
        }

        decimal composed = 100m;
        foreach (string link in links)
        {
            // Décimal exact : les pourcentages n'ont pas de représentation flottante binaire, et
            // la dérive se cumulerait au fil du produit.
            bool readable = decimal.TryParse(link, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
                && value > 0m && value <= 100m;
            if (!readable)
            {
                throw new ArgumentException(
                    "Un maillon de la chaîne est illisible ou hors bornes.", nameof(availabilities));
            }

            composed = composed * value / 100m;
        }

        // Plancher au centième, une seule fois : une disponibilité annoncée est un engagement vers
        // le bas, jamais flatté par l'arrondi.
        return Math.Floor(composed * 100m) / 100m;
    }
}
