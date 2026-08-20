using System;
using System.Collections.Generic;
using System.Globalization;

public static class Submission
{
    public static decimal LegacyBalance(string ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        decimal balance = 0m;

        // Pile des effets signes deja appliques : un void depile le dernier pour l'annuler.
        var appliedDeltas = new Stack<decimal>();

        foreach (string rawEntry in ledger.Split(';'))
        {
            string entry = rawEntry.Trim();
            if (entry.Length == 0)
            {
                continue;
            }

            if (entry == "void")
            {
                // Annule l'effet de la derniere entree appliquee, s'il y en a une.
                if (appliedDeltas.Count > 0)
                {
                    balance -= appliedDeltas.Pop();
                }

                continue;
            }

            int separator = entry.IndexOf(':');
            if (separator <= 0)
            {
                throw new ArgumentException("Entree de grand livre mal formee.", nameof(ledger));
            }

            string type = entry[..separator];
            string amountText = entry[(separator + 1)..];
            if (!decimal.TryParse(amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal amount))
            {
                throw new ArgumentException("Montant de grand livre illisible.", nameof(ledger));
            }

            // L'effet est signe : credit positif, debit negatif, pour qu'un void l'annule correctement.
            decimal delta = type switch
            {
                "credit" => amount,
                "debit" => -amount,
                _ => throw new ArgumentException("Type d'entree de grand livre inconnu.", nameof(ledger)),
            };

            balance += delta;
            appliedDeltas.Push(delta);
        }

        return balance;
    }
}
