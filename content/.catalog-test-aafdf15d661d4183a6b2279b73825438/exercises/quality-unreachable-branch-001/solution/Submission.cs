using System;
using System.Collections.Generic;
using System.Globalization;

public static class Submission
{
    // Le domaine analysé est celui du type entier signé, bornes comprises. Les calculs se font en
    // soixante-quatre bits : « la valeur juste après la plus grande » doit être représentable pour
    // que la comparaison reste juste, sans déborder.
    private const long DomainLow = int.MinValue;
    private const long DomainHigh = int.MaxValue;

    public static int FirstUnreachableCondition(string chain)
    {
        ArgumentNullException.ThrowIfNull(chain);

        string[] parts = chain.Split('|');
        var covered = new List<Interval>();
        int rank = 0;
        bool any = false;

        foreach (string rawPart in parts)
        {
            string part = rawPart.Trim();
            if (part.Length == 0)
            {
                continue;
            }

            any = true;
            rank++;
            Interval satisfied = Parse(part);

            // Un intervalle vide n'est satisfait par aucune valeur du domaine : la branche est morte
            // sans qu'aucune condition antérieure ait besoin de la couvrir.
            if (satisfied.Low > satisfied.High || IsCovered(satisfied, covered))
            {
                return rank;
            }

            Add(covered, satisfied);
        }

        if (!any)
        {
            throw new ArgumentException("La chaîne ne contient aucune condition.", nameof(chain));
        }

        return 0;
    }

    /// <summary>
    /// Vrai lorsque tout entier de l'intervalle appartient déjà à la réunion des intervalles couverts.
    /// </summary>
    /// <remarks>
    /// Les intervalles couverts sont maintenus triés et disjoints par <see cref="Add"/>. Le curseur
    /// avance donc en un seul passage : chaque intervalle qui contient le curseur le repousse au-delà
    /// de sa borne haute, et un trou se manifeste par un curseur qui n'avance plus.
    /// </remarks>
    private static bool IsCovered(Interval candidate, List<Interval> covered)
    {
        long cursor = candidate.Low;
        foreach (Interval interval in covered)
        {
            if (interval.Low > cursor)
            {
                break;
            }

            if (interval.High >= cursor)
            {
                cursor = interval.High + 1;
                if (cursor > candidate.High)
                {
                    return true;
                }
            }
        }

        return cursor > candidate.High;
    }

    /// <summary>Insère l'intervalle en conservant la liste triée, disjointe et fusionnée.</summary>
    private static void Add(List<Interval> covered, Interval interval)
    {
        long low = interval.Low;
        long high = interval.High;
        for (int index = covered.Count - 1; index >= 0; index--)
        {
            Interval existing = covered[index];

            // Deux intervalles fusionnent dès qu'ils se touchent, y compris bout à bout : sans cela,
            // la réunion garderait un trou de largeur nulle et le curseur s'y arrêterait.
            if (existing.Low <= high + 1 && low <= existing.High + 1)
            {
                low = Math.Min(low, existing.Low);
                high = Math.Max(high, existing.High);
                covered.RemoveAt(index);
            }
        }

        int position = 0;
        while (position < covered.Count && covered[position].Low < low)
        {
            position++;
        }

        covered.Insert(position, new Interval(low, high));
    }

    private static Interval Parse(string condition)
    {
        (string op, int skip) = condition switch
        {
            _ when condition.StartsWith("<=", StringComparison.Ordinal) => ("<=", 2),
            _ when condition.StartsWith(">=", StringComparison.Ordinal) => (">=", 2),
            _ when condition.StartsWith("==", StringComparison.Ordinal) => ("==", 2),
            _ when condition.StartsWith('<') => ("<", 1),
            _ when condition.StartsWith('>') => (">", 1),
            _ => throw new ArgumentException("Opérateur de condition inconnu.", nameof(condition)),
        };

        if (!int.TryParse(
                condition[skip..].Trim(),
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out int value))
        {
            throw new ArgumentException("Nombre de condition illisible.", nameof(condition));
        }

        long bound = value;
        return op switch
        {
            "<" => new Interval(DomainLow, bound - 1),
            "<=" => new Interval(DomainLow, bound),
            ">" => new Interval(bound + 1, DomainHigh),
            ">=" => new Interval(bound, DomainHigh),
            _ => new Interval(bound, bound),
        };
    }

    private readonly record struct Interval(long Low, long High);
}
